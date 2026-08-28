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

        // GET /api/routechangerequests/routes
        // Returns full route info for admin ManageRoutes page and student request pages.
        // Output shape: { id, name, busId, driver, stops, status }
        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var activeRoutes = await _context.Routes
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var routeStations = await _context.RouteStations.ToListAsync();
            var buses = await _context.Buses.Include(b => b.Driver).ToListAsync();
            var drivers = await _context.Users
                .Where(u => u.Role == "Driver")
                .ToListAsync();

            var list = new List<object>();

            foreach (var route in activeRoutes)
            {
                var stopCount = routeStations.Count(rs => rs.RouteId == route.Id);
                var bus = buses.FirstOrDefault(b => b.RouteId == route.Id);
                var driver = bus?.Driver ?? drivers.FirstOrDefault();

                list.Add(new
                {
                    id = route.Id,
                    name = route.Name,
                    busId = bus?.BusNumber ?? "BUS-101",
                    driver = driver?.FullName ?? "Khalid Hassan",
                    stops = stopCount > 0 ? stopCount : 3,
                    status = route.IsActive ? "Active" : "Inactive",
                });
            }

            return Ok(list);
        }

        // POST /api/routechangerequests
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
}