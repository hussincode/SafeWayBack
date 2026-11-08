using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Data;
using SafeWayAPI.Models;

namespace SafeWay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteChangeRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RouteChangeRequestsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/routechangerequests/stations
        // Returns full station info for admin ManageRoutes and student request pages.
        // Output shape: { id, name, address, students, scheduledTime, routes }
        [HttpGet("stations")]
        public async Task<IActionResult> GetStations()
        {
            var activeStations = await _context.Stations
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var routeStations = await _context.RouteStations
                .Include(rs => rs.Route)
                .ToListAsync();

            var list = new List<object>();

            foreach (var station in activeStations)
            {
                var matchingRouteStations = routeStations
                    .Where(rs => rs.StationId == station.Id && (rs.Route == null || rs.Route.IsActive))
                    .OrderBy(rs => rs.StopOrder)
                    .ToList();

                var scheduledTime = matchingRouteStations.FirstOrDefault() != null
                    ? matchingRouteStations.First().PickupTime.ToString(@"hh\:mm")
                    : "";

                var routes = matchingRouteStations
                    .Select(rs => rs.Route?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .Cast<string>()
                    .ToList();

                list.Add(new
                {
                    id = station.Id,
                    name = station.Name,
                    address = station.Name,
                    students = 1,
                    scheduledTime = scheduledTime,
                    routes = routes,
                });
            }

            return Ok(list);
        }

        // GET /api/routechangerequests/drivers
        // Returns list of all drivers with their assigned bus and route
        [HttpGet("drivers")]
        public async Task<IActionResult> GetDriversForRoutes()
        {
            var drivers = await _context.Users
                .Where(u => u.Role == "Driver")
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.FullName,
                    phone = u.Phone,
                    busNumber = _context.Buses.Where(b => b.DriverId == u.Id).Select(b => b.BusNumber).FirstOrDefault() ?? "No Bus",
                    currentRouteId = _context.Buses.Where(b => b.DriverId == u.Id).Select(b => b.RouteId).FirstOrDefault(),
                })
                .ToListAsync();

            return Ok(drivers);
        }

        // GET /api/routechangerequests/routes
        // Returns full route info for admin ManageRoutes page and student request pages.
        // Output shape: { id, name, busId, driver, drivers, driverIds, stops, status }
        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var activeRoutes = await _context.Routes
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var routeStations = await _context.RouteStations.ToListAsync();
            var buses = await _context.Buses.Include(b => b.Driver).ToListAsync();

            var list = new List<object>();

            foreach (var route in activeRoutes)
            {
                var stopCount = routeStations.Count(rs => rs.RouteId == route.Id);
                var routeBuses = buses.Where(b => b.RouteId == route.Id).ToList();

                var routeDrivers = routeBuses
                    .Where(b => b.Driver != null)
                    .Select(b => b.Driver!.FullName)
                    .Distinct()
                    .ToList();

                var driverIds = routeBuses
                    .Where(b => b.DriverId != null)
                    .Select(b => b.DriverId!.Value)
                    .Distinct()
                    .ToList();

                var driverDisplay = routeDrivers.Any()
                    ? string.Join(", ", routeDrivers)
                    : "Not assigned";

                var busDisplay = routeBuses.Any()
                    ? string.Join(", ", routeBuses.Select(b => b.BusNumber))
                    : "Not assigned";

                list.Add(new
                {
                    id = route.Id,
                    name = route.Name,
                    busId = busDisplay,
                    driver = driverDisplay,
                    drivers = routeDrivers,
                    driverIds = driverIds,
                    stops = stopCount > 0 ? stopCount : 3,
                    status = route.IsActive ? "Active" : "Inactive",
                });
            }

            return Ok(list);
        }

        // PUT /api/routechangerequests/routes/{id}
        // Body: { name, driverIds, driverNames, busNumber }
        [HttpPut("routes/{id:int}")]
        public async Task<IActionResult> UpdateRoute([FromRoute] int id, [FromBody] UpdateRouteDto dto)
        {
            try
            {
                var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == id);
                if (route == null)
                    return NotFound(new { message = "Route not found" });

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    route.Name = dto.Name.Trim();

                if (dto.DriverIds != null)
                {
                    // Unlink previous buses whose drivers are not in the selected list
                    var currentBuses = await _context.Buses.Where(b => b.RouteId == id).ToListAsync();
                    foreach (var bus in currentBuses)
                    {
                        if (bus.DriverId == null || !dto.DriverIds.Contains(bus.DriverId.Value))
                        {
                            bus.RouteId = null;
                        }
                    }

                    // Assign buses belonging to selected drivers to this route
                    var busesToAssign = await _context.Buses
                        .Where(b => b.DriverId != null && dto.DriverIds.Contains(b.DriverId.Value))
                        .ToListAsync();
                    foreach (var bus in busesToAssign)
                    {
                        bus.RouteId = id;
                    }
                }
                else if (dto.DriverNames != null)
                {
                    var driverUsers = await _context.Users
                        .Where(u => u.Role == "Driver" && dto.DriverNames.Contains(u.FullName))
                        .Select(u => u.Id)
                        .ToListAsync();

                    var currentBuses = await _context.Buses.Where(b => b.RouteId == id).ToListAsync();
                    foreach (var bus in currentBuses)
                    {
                        if (bus.DriverId == null || !driverUsers.Contains(bus.DriverId.Value))
                        {
                            bus.RouteId = null;
                        }
                    }

                    var busesToAssign = await _context.Buses
                        .Where(b => b.DriverId != null && driverUsers.Contains(b.DriverId.Value))
                        .ToListAsync();
                    foreach (var bus in busesToAssign)
                    {
                        bus.RouteId = id;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(dto.BusNumber))
                {
                    var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == dto.BusNumber.Trim());
                    if (bus != null)
                    {
                        bus.RouteId = route.Id;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Route updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating route", error = ex.Message });
            }
        }

        // POST /api/routechangerequests/routes
        // Body: { name, driverIds, driverNames, busNumber }
        [HttpPost("routes")]
        public async Task<IActionResult> CreateRoute([FromBody] UpdateRouteDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { message = "Route name is required" });

                var newRoute = new BusRoute
                {
                    Name = dto.Name.Trim(),
                    IsActive = true
                };

                _context.Routes.Add(newRoute);
                await _context.SaveChangesAsync();

                if (dto.DriverIds != null && dto.DriverIds.Any())
                {
                    var busesToAssign = await _context.Buses
                        .Where(b => b.DriverId != null && dto.DriverIds.Contains(b.DriverId.Value))
                        .ToListAsync();
                    foreach (var bus in busesToAssign)
                    {
                        bus.RouteId = newRoute.Id;
                    }
                    await _context.SaveChangesAsync();
                }
                else if (dto.DriverNames != null && dto.DriverNames.Any())
                {
                    var driverUsers = await _context.Users
                        .Where(u => u.Role == "Driver" && dto.DriverNames.Contains(u.FullName))
                        .Select(u => u.Id)
                        .ToListAsync();

                    var busesToAssign = await _context.Buses
                        .Where(b => b.DriverId != null && driverUsers.Contains(b.DriverId.Value))
                        .ToListAsync();
                    foreach (var bus in busesToAssign)
                    {
                        bus.RouteId = newRoute.Id;
                    }
                    await _context.SaveChangesAsync();
                }
                else if (!string.IsNullOrWhiteSpace(dto.BusNumber))
                {
                    var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == dto.BusNumber.Trim());
                    if (bus != null)
                    {
                        bus.RouteId = newRoute.Id;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { id = newRoute.Id, message = "Route created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating route", error = ex.Message });
            }
        }

        // DELETE /api/routechangerequests/routes/{id}
        [HttpDelete("routes/{id:int}")]
        public async Task<IActionResult> DeleteRoute([FromRoute] int id)
        {
            try
            {
                var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == id);
                if (route == null)
                    return NotFound(new { message = "Route not found" });

                // Unlink any bus linked to this route
                var buses = await _context.Buses.Where(b => b.RouteId == id).ToListAsync();
                foreach (var bus in buses)
                {
                    bus.RouteId = null;
                }

                // Soft-delete: mark inactive instead of removing (preserves referential integrity)
                route.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Route deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting route", error = ex.Message });
            }
        }


        // Body: { userId, newStationId, newRouteId, effectiveDate }
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] RouteChangeRequestDto dto)
        {
            if (dto.UserId == 0 || dto.NewStationId == 0 || dto.NewRouteId == 0 || dto.EffectiveDate == default)
                return BadRequest(new { message = "userId, newStationId, newRouteId and effectiveDate are all required." });

            var existing = await _context.RouteChangeRequests
                .AnyAsync(r => r.UserId == dto.UserId && r.Status == "PENDING");

            if (existing)
                return Conflict(new { message = "You already have a pending route change request. Please wait for it to be reviewed." });

            var entity = new RouteChangeRequest
            {
                UserId = dto.UserId,
                NewStationId = dto.NewStationId,
                NewRouteId = dto.NewRouteId,
                EffectiveDate = dto.EffectiveDate,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
            };

            _context.RouteChangeRequests.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { id = entity.Id, message = "Route change request submitted successfully." });
        }

        // GET /api/routechangerequests/user/{userId}
        // Returns request history for a student
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var requests = await _context.RouteChangeRequests
                .Include(r => r.NewStation)
                .Include(r => r.NewRoute)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    stationName = r.NewStation != null ? r.NewStation.Name : "Station",
                    routeName = r.NewRoute != null ? r.NewRoute.Name : "Route",
                    effectiveDate = r.EffectiveDate.ToString("yyyy-MM-dd"),
                    status = r.Status,
                    adminNote = r.AdminNote,
                    createdAt = r.CreatedAt.ToString("yyyy-MM-dd"),
                })
                .ToListAsync();

            return Ok(requests);
        }
    }

    public class RouteChangeRequestDto
    {
        public int UserId { get; set; }
        public int NewStationId { get; set; }
        public int NewRouteId { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class UpdateRouteDto
    {
        public string? Name { get; set; }
        public string? BusNumber { get; set; }
        public string? DriverName { get; set; }
        public List<string>? DriverNames { get; set; }
        public List<int>? DriverIds { get; set; }
    }
}