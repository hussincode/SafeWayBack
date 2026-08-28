using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SafeWayAPI.Data;
using SafeWayAPI.DTOs;

namespace SafeWayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UniqueID == request.UniqueID);

            if (user == null)
                return Unauthorized(new { message = "ID not found." });

            // Check if password matches (plaintext or BCrypt hash)
            bool isCorrect = request.Password == user.Password;
            if (!isCorrect && !string.IsNullOrEmpty(user.Password))
            {
                try
                {
                    var storedHash = NormalizeBcryptHash(user.Password);
                    isCorrect = BCrypt.Net.BCrypt.Verify(request.Password, storedHash);
                }
                catch
                {
                    isCorrect = false;
                }
            }

            if (!isCorrect)
                return Unauthorized(new { message = "Wrong password." });

            var token = GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Id       = user.Id,
                FullName = user.FullName,
                   
                Role = user.Role,
                UniqueID = user.UniqueID

            });
        }

        // Add this inside AuthController class, after GetStudentInfo

[HttpGet("driver-info/{userId}")]
public IActionResult GetDriverInfo(int userId)
{
    var driver = _context.Users
        .FirstOrDefault(u => u.Id == userId && u.Role == "Driver");

    if (driver == null)
        return NotFound(new { message = "Driver not found" });

    var bus = _context.Buses.FirstOrDefault(b => b.DriverId == userId);
    var route = bus != null && bus.RouteId.HasValue 
        ? _context.Routes.FirstOrDefault(r => r.Id == bus.RouteId.Value) 
        : _context.Routes.FirstOrDefault();

    var busNumber = bus?.BusNumber ?? "BUS-101";
    var routeName = route?.Name ?? "Route A - Downtown";

    var studentUserIds = bus != null 
        ? _context.Students.Where(s => s.BusId == bus.Id).Select(s => s.UserId).ToList()
        : _context.Users.Where(u => u.Role == "Student").Select(u => u.Id).ToList();

    if (!studentUserIds.Any())
    {
        studentUserIds = _context.Users.Where(u => u.Role == "Student").Select(u => u.Id).ToList();
    }

    var students = _context.Users.Where(u => studentUserIds.Contains(u.Id)).ToList();

    var studentData = students.Select(student => {
        var sub = _context.Subscriptions
            .Where(s => s.UserId == student.Id)
            .OrderByDescending(s => s.Id)
            .FirstOrDefault();

        return new {
            id            = student.Id,
            fullName      = student.FullName,
            grade         = "Grade 10",
            stopName      = "Main Street Station",
            paymentStatus = sub?.Status ?? "UNPAID",
        };
    }).ToList();

    return Ok(new {
        fullName      = driver.FullName,
        uniqueID      = driver.UniqueID,
        busNumber     = busNumber,
        routeName     = routeName,
        totalStudents = studentData.Count,
        paidCount     = studentData.Count(s => s.paymentStatus == "PAID"),
        unpaidCount   = studentData.Count(s => s.paymentStatus == "UNPAID"),
        expiredCount  = studentData.Count(s => s.paymentStatus == "EXPIRED"),
        students      = studentData,
    });
}



        [HttpGet("parent-info/{parentId}")]
        public IActionResult GetParentInfo(int parentId)
        {
            var parent = _context.Users.FirstOrDefault(u => u.Id == parentId);
            if (parent == null)
                return NotFound(new { message = "Parent not found" });

            // Get student UserIds linked to this parent from students table
            var childUserIds = _context.Students
                .Where(s => s.ParentId == parentId)
                .Select(s => s.UserId)
                .ToList();

            if (!childUserIds.Any())
            {
                // Fallback to all student users if none explicitly linked
                childUserIds = _context.Users
                    .Where(u => u.Role == "Student")
                    .Select(u => u.Id)
                    .Take(1)
                    .ToList();
            }

            var children = _context.Users
                .Where(u => childUserIds.Contains(u.Id))
                .ToList();

            var childrenData = children.Select(child => {
                var sub = _context.Subscriptions
                    .Where(s => s.UserId == child.Id)
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefault();

                return new {
                    name          = child.FullName,
                    grade         = child.Grade ?? "Grade 10",
                    busNumber     = "BUS-101",
                    eta           = "5 min",
                    pickupStation = "Main Street Station",
                    subscription  = sub?.Status ?? "UNPAID",
                    isOnBoard     = false,
                    boardingNote  = (string?)null,
                };
            }).ToList();

            return Ok(new {
                fullName        = parent.FullName,
                uniqueID        = parent.UniqueID,
                children        = childrenData,
                onBoardCount    = 0,
                activeSubsCount = childrenData.Count(c => c.subscription == "PAID"),
                totalChildren   = childrenData.Count,
            });
        }

// Add this inside AuthController, after GetDriverInfo

[HttpGet("driver-route/{userId}")]
public IActionResult GetDriverRoute(int userId)
{
    var driver = _context.Users
        .FirstOrDefault(u => u.Id == userId && u.Role == "Driver");

    if (driver == null)
        return NotFound(new { message = "Driver not found" });

    var bus = _context.Buses.FirstOrDefault(b => b.DriverId == userId);
    var route = bus != null && bus.RouteId.HasValue 
        ? _context.Routes.FirstOrDefault(r => r.Id == bus.RouteId.Value) 
        : _context.Routes.FirstOrDefault();

    if (route == null)
        return NotFound(new { message = "Route not found for this driver" });

    var busNumber = bus?.BusNumber ?? "BUS-101";

    var routeStations = _context.RouteStations
        .Include(rs => rs.Station)
        .Where(rs => rs.RouteId == route.Id)
        .OrderBy(rs => rs.StopOrder)
        .ToList();

    var stops = routeStations.Select(rs => new {
        stopOrder = rs.StopOrder,
        pickupTime = rs.PickupTime.ToString(@"hh\:mm"),
        station = new {
            id = rs.Station != null ? rs.Station.Id : rs.StationId,
            name = rs.Station != null ? rs.Station.Name : "Station",
        },
        students = _context.Users
            .Where(u => u.Role == "Student")
            .Select(u => new {
                id = u.Id,
                fullName = u.FullName,
                grade = "Grade 10",
                paymentStatus = _context.Subscriptions
                    .Where(s => s.UserId == u.Id)
                    .OrderByDescending(s => s.Id)
                    .Select(s => s.Status)
                    .FirstOrDefault() ?? "UNPAID",
            })
            .ToList()
    }).ToList();

    return Ok(new {
        routeName = route.Name,
        busNumber = busNumber,
        totalStops = stops.Count,
        stops = stops,
    });
}

        [HttpGet("setup")]
        public async Task<IActionResult> Setup()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                // Avoid re-hashing an already hashed password.
                // BCrypt hashes start with $2a$, $2b$, $2y$, etc.
                if (!user.Password.StartsWith("$2"))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                }
            }
            await _context.SaveChangesAsync();
            return Ok("Passwords hashed!");
        }

        

        private static string NormalizeBcryptHash(string hash)
        {
            if (hash.StartsWith("$2y$") || hash.StartsWith("$2x$"))
            {
                return "$2a$" + hash.Substring(4);
            }

            return hash;
        }

        private string GenerateToken(SafeWayAPI.Models.User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("uid", user.UniqueID),
                new Claim("name", user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(
                    double.Parse(_config["JwtSettings:ExpiryDays"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        [HttpGet("student-info/{userId}")]
        public async Task<IActionResult> GetStudentInfo(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                fullName = user.FullName,
                uniqueID = user.UniqueID,
                grade = "Grade 10",
                busNumber = "BUS-101",
                driverName = "Khalid Hassan",
                routeName = "Route A - Downtown",
                stopName = "Main Street Station"
            });
        }

    }  
}