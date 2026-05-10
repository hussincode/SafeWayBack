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

                var busNumber = (dto.BusNumber ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(busNumber))
                    return BadRequest(new { message = "BusNumber is required" });

                var routeName = (dto.RouteName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(routeName))
                    return BadRequest(new { message = "RouteName is required" });

                var phone = (dto.Phone ?? string.Empty).Trim();

                // Create a new driver user
                var driver = new Models.User
                {
                    FullName = fullName,
                    Role = "Driver",
                    BusNumber = busNumber,
                    Phone = phone,
                    RouteName = routeName,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active",
                    // Generate UniqueID format: DRV001, DRV002, ... (no repeat)
                    UniqueID = GenerateNextDriverUniqueId(),
                };

                // Password format: drv001pass (matches the number in UniqueID)
                driver.Password = GenerateDriverPasswordFromUniqueId(driver.UniqueID);

                _context.Users.Add(driver);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Driver added successfully",
                    driver = new
                    {
                        driver.UniqueID,
                        driver.Id,
                        driver.FullName,
                        driver.BusNumber,
                        driver.Phone,
                        driver.RouteName,
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
                var drivers = await _context.Users
                    .Where(u => u.Role == "Driver")
                    .OrderBy(u => u.FullName)
                    .Select(u => new DriverRecordDto
                    {
                        Id = u.Id,
                        DriverId = string.IsNullOrWhiteSpace(u.UniqueID) ? "" : u.UniqueID,
                        FullName = u.FullName ?? string.Empty,
                        Email = "", // backend does not have email field in User model
                        Phone = u.Phone ?? string.Empty, // backend does not have phone field in User model
                        BusId = u.BusNumber ?? string.Empty,
                        Route = u.RouteName ?? string.Empty,
                        Status = "Active" // TODO: map to real active/inactive field when available
                    })
                    .ToListAsync();

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

                var busNumber = (dto.BusNumber ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(busNumber))
                    return BadRequest(new { message = "BusNumber is required" });

                var routeName = (dto.RouteName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(routeName))
                    return BadRequest(new { message = "RouteName is required" });

                var phone = (dto.Phone ?? string.Empty).Trim();

                driver.FullName = fullName;
                driver.BusNumber = busNumber;
                driver.Phone = phone;
                driver.RouteName = routeName;

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


        // GET /api/admin/students
        [HttpGet("students")]
        public async Task<ActionResult<List<StudentRecordDto>>> GetStudents()
        {
            try
            {

                // For each student, return latest subscription status.
                // NOTE: This uses two queries for clarity; it can be optimized later.
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

            // Active Buses - Count distinct bus numbers
            var activeBuses = await _context.Users
                .Where(u => u.Role == "Driver" && !string.IsNullOrEmpty(u.BusNumber))
                .Select(u => u.BusNumber)
                .Distinct()
                .CountAsync();

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
                .Where(s => s.StartDate.Date == today && s.Status == "Active")
                .CountAsync();

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

            // Get distinct bus numbers with driver info
            var driverBuses = await _context.Users
                .Where(u => u.Role == "Driver" && !string.IsNullOrEmpty(u.BusNumber))
                .GroupBy(u => u.BusNumber)
                .Select(g => new
                {
                    BusNumber = g.Key,
                    Driver = g.Select(x => x.FullName).FirstOrDefault() ?? "No Driver",
                    Route = g.Select(x => x.RouteName).FirstOrDefault(r => !string.IsNullOrEmpty(r)) ?? "Route Unknown"
                })
                .ToListAsync();

            var colors = new[] { "#4F46E5", "#16A34A", "#3B82F6", "#F59E0B" };
            double baseLat = 30.0444;
            double baseLng = 31.2357;

            for (int i = 0; i < driverBuses.Count; i++)
            {
                var bus = driverBuses[i];
                var studentCount = await _context.Users
                    .Where(u => u.BusNumber == bus.BusNumber && u.Role == "Student")
                    .CountAsync();

                buses.Add(new BusDashboardDto
                {
                    Id = $"BUS-{(101 + i):D3}",
                    Driver = bus.Driver,
                    Route = bus.Route,
                    Occupancy = $"{studentCount}/40",
                    NextStop = GetNextStop(i),
                    Status = "Active",
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
                    Driver = "No Drivers",
                    Route = "No Routes",
                    Occupancy = "0/40",
                    NextStop = "No Stop",
                    Status = "Inactive",
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
