using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Data;
using SafeWayAPI.DTOs;

namespace SafeWayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("dashboard-summary")]
        public async Task<ActionResult<AdminDashboardSummaryDto>> GetDashboardSummary()
        {
            try
            {
                var summary = new AdminDashboardSummaryDto();

                // Get Stats
                summary.Stats = await GetStatistics();

                // Get Buses
                summary.Buses = await GetBusesInfo();

                // Get Activities
                summary.Activities = await GetRecentActivities();

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardSummary: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching dashboard summary", error = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<List<StatCardDto>>> GetStats()
        {
            try
            {
                var stats = await GetStatistics();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetStats: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching stats", error = ex.Message });
            }
        }

        [HttpGet("buses")]
        public async Task<ActionResult<List<BusDashboardDto>>> GetBuses()
        {
            try
            {
                var buses = await GetBusesInfo();
                return Ok(buses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetBuses: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching buses", error = ex.Message });
            }
        }

        [HttpGet("activities")]
        public async Task<ActionResult<List<ActivityDto>>> GetActivities()
        {
            try
            {
                var activities = await GetRecentActivities();
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetActivities: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching activities", error = ex.Message });
            }
        }

        // Add driver
        // POST /api/admin/drivers
        // Add driver
        // POST /api/admin/drivers
        [HttpPost("drivers")]
        public async Task<ActionResult> AddDriver([FromBody] AddDriverRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var fullName = (dto.FullName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(fullName))
                    return BadRequest(new { message = "FullName is required" });

                var busNumber = (dto.BusNumber ?? "BUS-101").Trim();
                var routeName = (dto.RouteName ?? "Route A - Downtown").Trim();
                var phone = (dto.Phone ?? string.Empty).Trim();

                var driver = new Models.User
                {
                    FullName = fullName,
                    Role = "Driver",
                    Phone = phone,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active",
                    UniqueID = GenerateNextDriverUniqueId(),
                };

                driver.Password = GenerateDriverPasswordFromUniqueId(driver.UniqueID);

                _context.Users.Add(driver);
                await _context.SaveChangesAsync();

                // Link bus and route in DB
                var route = await _context.Routes.FirstOrDefaultAsync(r => r.Name == routeName);
                if (route == null)
                {
                    route = new Models.BusRoute { Name = routeName, IsActive = true };
                    _context.Routes.Add(route);
                    await _context.SaveChangesAsync();
                }

                var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == busNumber);
                if (bus == null)
                {
                    bus = new Models.Bus { BusNumber = busNumber, DriverId = driver.Id, RouteId = route.Id, IsActive = true };
                    _context.Buses.Add(bus);
                }
                else
                {
                    bus.DriverId = driver.Id;
                    bus.RouteId = route.Id;
                }
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Driver added successfully",
                    driver = new
                    {
                        driver.UniqueID,
                        driver.Id,
                        driver.FullName,
                        BusNumber = busNumber,
                        Phone = phone,
                        RouteName = routeName,
                        driver.Status
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error in AddDriver (DbUpdateException)");
                return StatusCode(500, new { message = "Error adding driver (db)", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddDriver");
                return StatusCode(500, new { message = "Error adding driver", error = ex.Message });
            }
        }


        [HttpGet("drivers")]
        public async Task<ActionResult<List<DriverRecordDto>>> GetDrivers()
        {
            try
            {
                var drivers = await (from u in _context.Users
                                     where u.Role == "Driver"
                                     join b in _context.Buses on u.Id equals b.DriverId into busGroup
                                     from b in busGroup.DefaultIfEmpty()
                                     join r in _context.Routes on (b != null ? b.RouteId : (int?)null) equals r.Id into routeGroup
                                     from r in routeGroup.DefaultIfEmpty()
                                     orderby u.FullName
                                     select new DriverRecordDto
                                     {
                                         Id = u.Id,
                                         DriverId = string.IsNullOrWhiteSpace(u.UniqueID) ? $"DRV{u.Id:D3}" : u.UniqueID,
                                         FullName = u.FullName ?? string.Empty,
                                         Email = "",
                                         Phone = u.Phone ?? string.Empty,
                                         BusId = b != null ? b.BusNumber : "BUS-101",
                                         Route = r != null ? r.Name : "Route A - Downtown",
                                         Status = u.Status ?? "Active"
                                     }).ToListAsync();

                return Ok(drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDrivers: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching drivers", error = ex.Message });
            }
        }

        // PUT /api/admin/drivers/{id}
        [HttpPut("drivers/{id:int}")]
        public async Task<ActionResult> UpdateDriver([FromRoute] int id, [FromBody] AddDriverRequestDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var driver = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Driver");
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                var fullName = (dto.FullName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(fullName))
                    return BadRequest(new { message = "FullName is required" });

                var busNumber = (dto.BusNumber ?? "BUS-101").Trim();
                var routeName = (dto.RouteName ?? "Route A - Downtown").Trim();
                var phone = (dto.Phone ?? string.Empty).Trim();

                driver.FullName = fullName;
                driver.Phone = phone;

                var route = await _context.Routes.FirstOrDefaultAsync(r => r.Name == routeName);
                if (route == null)
                {
                    route = new Models.BusRoute { Name = routeName, IsActive = true };
                    _context.Routes.Add(route);
                    await _context.SaveChangesAsync();
                }

                var bus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == busNumber);
                if (bus == null)
                {
                    bus = new Models.Bus { BusNumber = busNumber, DriverId = driver.Id, RouteId = route.Id, IsActive = true };
                    _context.Buses.Add(bus);
                }
                else
                {
                    bus.DriverId = driver.Id;
                    bus.RouteId = route.Id;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Driver updated successfully" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error in UpdateDriver (DbUpdateException)");
                return StatusCode(500, new { message = "Error updating driver (db)", error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateDriver");
                return StatusCode(500, new { message = "Error updating driver", error = ex.Message });
            }
        }


        // DELETE /api/admin/drivers/{id}
        [HttpDelete("drivers/{id:int}")]
        public async Task<ActionResult> DeleteDriver([FromRoute] int id)
        {
            try
            {
                var driver = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Driver");
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                // 1. Instantly unassign driver from buses table in SQL Server
                await _context.Database.ExecuteSqlRawAsync("UPDATE buses SET driverid = NULL WHERE driverid = {0}", id);

                // 2. Remove driver user row
                _context.Users.Remove(driver);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Driver deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteDriver");
                return StatusCode(500, new { message = "Error deleting driver", error = ex.Message });
            }
        }

        // POST /api/admin/students
        [HttpPost("students")]
        public async Task<ActionResult> AddStudent([FromBody] StudentRecordDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Request body is required" });

                var fullName = (dto.FullName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(fullName))
                    return BadRequest(new { message = "FullName is required" });

                var uniqueID = (dto.UniqueID ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(uniqueID))
                {
                    var count = await _context.Users.CountAsync(u => u.Role == "Student");
                    uniqueID = $"STU{(count + 1):D3}";
                }

                var student = new Models.User
                {
                    FullName = fullName,
                    UniqueID = uniqueID,
                    Password = "Student123",
                    Role = "Student",
                    BusNumber = dto.BusNumber ?? "BUS-101",
                    RouteName = dto.RouteName ?? "Route A - Downtown",
                    Grade = dto.Grade ?? "Grade 10",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active",
                };

                _context.Users.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Student added successfully", student });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddStudent");
                return StatusCode(500, new { message = "Error adding student", error = ex.Message });
            }
        }

        // GET /api/admin/students
        [HttpGet("students")]
        public async Task<ActionResult<List<StudentRecordDto>>> GetStudents()
        {
            try
            {
                // For each student, return latest subscription status.
                var students = await _context.Users
                    .Where(u => u.Role == "Student")
                    .OrderBy(u => u.FullName)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.UniqueID,
                        u.BusNumber,
                        u.RouteName,
                        u.Grade
                    })
                    .ToListAsync();

                var studentIds = students.Select(s => s.Id).ToList();

                var latestSubscriptions = await _context.Subscriptions
                    .Where(s => studentIds.Contains(s.UserId))
                    .GroupBy(s => s.UserId)
                    .Select(g => g.OrderByDescending(x => x.Id).FirstOrDefault()!)
                    .ToListAsync();

                var subByUserId = latestSubscriptions.ToDictionary(s => s.UserId, s => s.Status);

                var result = students.Select(s => new StudentRecordDto
                {
                    Id = s.Id,
                    FullName = s.FullName ?? string.Empty,
                    UniqueID = s.UniqueID ?? string.Empty,
                    BusNumber = s.BusNumber ?? "Not assigned",
                    RouteName = s.RouteName ?? "Not assigned",
                    Grade = s.Grade ?? string.Empty,
                    SubscriptionStatus = subByUserId.TryGetValue(s.Id, out var status)
                        ? status ?? "UNPAID"
                        : "UNPAID"
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetStudents: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching students", error = ex.Message });
            }
        }


        private async Task<List<StatCardDto>> GetStatistics()
        {
            var stats = new List<StatCardDto>();

            // Total Students - Count active users with student role
            var totalStudents = await _context.Users
                .Where(u => u.Role == "Student")
                .CountAsync();

            stats.Add(new StatCardDto
            {
                Label = "Total Students",
                Value = totalStudents.ToString(),
                Sub = "+12% this month",
                SubColor = "#16A34A",
                Icon = "group",
                IconBg = "#EFF6FF",
                IconColor = "#3B82F6",
                BorderColor = "#3B82F6"
            });

            // Active Buses - Count active buses
            var activeBuses = await _context.Buses
                .Where(b => b.IsActive)
                .CountAsync();

            if (activeBuses == 0) activeBuses = 1;

            stats.Add(new StatCardDto
            {
                Label = "Active Buses",
                Value = activeBuses.ToString(),
                Sub = "All operational",
                SubColor = "#6B7280",
                Icon = "directions_bus",
                IconBg = "#FEF3C7",
                IconColor = "#F59E0B",
                BorderColor = "#F59E0B"
            });

            // Total Drivers
            var totalDrivers = await _context.Users
                .Where(u => u.Role == "Driver")
                .CountAsync();

            stats.Add(new StatCardDto
            {
                Label = "Total Drivers",
                Value = totalDrivers.ToString(),
                Sub = "All active",
                SubColor = "#16A34A",
                Icon = "person",
                IconBg = "#F0FFF4",
                IconColor = "#16A34A",
                BorderColor = "#16A34A"
            });

            // Today's Trips - Count subscriptions for today
            var today = DateTime.UtcNow.Date;
            var todaysTrips = await _context.Subscriptions
                .Where(s => s.StartDate.Date <= today && s.EndDate.Date >= today)
                .CountAsync();

            if (todaysTrips == 0) todaysTrips = 1;

            stats.Add(new StatCardDto
            {
                Label = "Today's Trips",
                Value = (todaysTrips * 12).ToString(), // Estimate: 12 trips per subscription
                Sub = "On schedule",
                SubColor = "#8B5CF6",
                Icon = "alt_route",
                IconBg = "#F5F3FF",
                IconColor = "#8B5CF6",
                BorderColor = "#8B5CF6"
            });

            return stats;
        }

        private async Task<List<BusDashboardDto>> GetBusesInfo()
        {
            var buses = new List<BusDashboardDto>();

            var dbBuses = await _context.Buses
                .Include(b => b.Driver)
                .Include(b => b.Route)
                .ToListAsync();

            var colors = new[] { "#4F46E5", "#16A34A", "#3B82F6", "#F59E0B" };
            double baseLat = 30.0444;
            double baseLng = 31.2357;

            for (int i = 0; i < dbBuses.Count; i++)
            {
                var bus = dbBuses[i];
                var studentCount = await _context.Students
                    .Where(s => s.BusId == bus.Id)
                    .CountAsync();

                buses.Add(new BusDashboardDto
                {
                    Id = bus.BusNumber,
                    Driver = bus.Driver?.FullName ?? "Khalid Hassan",
                    Route = bus.Route?.Name ?? "Route A - Downtown",
                    Occupancy = $"{studentCount}/40",
                    NextStop = GetNextStop(i),
                    Status = bus.IsActive ? "Active" : "Inactive",
                    Latitude = baseLat + (i * 0.005),
                    Longitude = baseLng + (i * 0.005),
                    Color = colors[i % colors.Length]
                });
            }

            // If no buses, add demo buses
            if (buses.Count == 0)
            {
                buses.Add(new BusDashboardDto
                {
                    Id = "BUS-101",
                    Driver = "Khalid Hassan",
                    Route = "Route A - Downtown",
                    Occupancy = "1/40",
                    NextStop = "Main Street Station (5 min)",
                    Status = "Active",
                    Latitude = 30.0444,
                    Longitude = 31.2357,
                    Color = "#4F46E5"
                });
            }

            return buses;
        }

        private async Task<List<ActivityDto>> GetRecentActivities()
        {
            var activities = new List<ActivityDto>();

            // Get recent subscriptions (boarding)
            var recentSubscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .OrderByDescending(s => s.StartDate)
                .Take(5)
                .ToListAsync();

            foreach (var sub in recentSubscriptions)
            {
                activities.Add(new ActivityDto
                {
                    Type = "Boarding",
                    Icon = "check_circle",
                    Color = "#16A34A",
                    Details = sub.User?.FullName ?? "Unknown Student",
                    Bus = sub.User?.BusNumber ?? "Unknown"
                });
            }

            // If no activities, add demo
            if (activities.Count == 0)
            {
                activities.Add(new ActivityDto
                {
                    Type = "Boarding",
                    Icon = "check_circle",
                    Color = "#16A34A",
                    Details = "No Recent Activity",
                    Bus = "---"
                });
            }

            return activities;
        }

        private string GetNextStop(int index)
        {
            var stops = new[] { "Main Street Station (5 min)", "Oak Street Station (8 min)", "Central Plaza (10 min)", "North Terminal (7 min)" };
            return stops[index % stops.Length];
        }

        private string GenerateNextDriverUniqueId()
        {
     var existing = _context.Users
        .Where(u => u.Role == "Driver" && u.UniqueID != null && u.UniqueID.StartsWith("DRV"))
        .Select(u => u.UniqueID)
        .ToList();

    var usedNumbers = existing
        .Select(u => u.Length >= 6 ? u.Substring(3) : "")
        .Where(s => int.TryParse(s, out _))
        .Select(int.Parse)
        .ToHashSet();

    var next = 1;
    while (usedNumbers.Contains(next)) next++;

    // enforce exactly DRV001 format
    return $"DRV{next:000}";
        }

        private string GenerateDriverPasswordFromUniqueId(string uniqueId)
        {
    if (string.IsNullOrWhiteSpace(uniqueId)) 
        return string.Empty;

    var numberPart = uniqueId.StartsWith("DRV") 
        ? uniqueId.Substring(3) 
        : uniqueId;

    return $"drv{numberPart}pass";
        }
    }
}
