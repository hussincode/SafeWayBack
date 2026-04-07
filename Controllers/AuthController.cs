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

            // Try BCrypt verification first, fall back to plaintext for testing
            bool isCorrect = false;
            try
            {
                var storedHash = NormalizeBcryptHash(user.Password);
                isCorrect = BCrypt.Net.BCrypt.Verify(request.Password, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // If BCrypt fails (plaintext password), do plaintext comparison for testing
                isCorrect = request.Password == user.Password;
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

    // Get all students assigned to this driver's bus
    var students = _context.Users
        .Where(u => u.BusNumber == driver.BusNumber && u.Role == "Student")
        .ToList();

    var studentData = students.Select(student => {
        var sub = _context.Subscriptions
            .Where(s => s.UserId == student.Id)
            .OrderByDescending(s => s.Id)
            .FirstOrDefault();

        return new {
            id            = student.Id,
            fullName      = student.FullName,
            grade         = student.Grade ?? "",
            stopName      = student.StopName ?? "Not assigned",
            paymentStatus = sub?.Status ?? "UNPAID",
        };
    }).ToList();

    return Ok(new {
        fullName      = driver.FullName,
        uniqueID      = driver.UniqueID,
        busNumber     = driver.BusNumber  ?? "Not assigned",
        routeName     = driver.RouteName  ?? "Not assigned",
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

    // Get all children linked to this parent
    var children = _context.Users
        .Where(u => u.ParentId == parentId)
        .ToList();

    var childrenData = children.Select(child => {
        var sub = _context.Subscriptions
            .Where(s => s.UserId == child.Id)
            .OrderByDescending(s => s.Id)
            .FirstOrDefault();

        return new {
            name         = child.FullName,
            grade        = child.Grade ?? "",
            busNumber    = "BUS-101",
            eta          = "5 min",
            pickupStation = "Main Street Station",
            subscription = sub?.Status ?? "UNPAID",
            isOnBoard    = false,
            boardingNote = (string?)null,
        };
    }).ToList();

    return Ok(new {
        fullName  = parent.FullName,
        uniqueID  = parent.UniqueID,
        children  = childrenData,
        onBoardCount      = 0,
        activeSubsCount   = childrenData.Count(c => c.subscription == "PAID"),
        totalChildren     = childrenData.Count,
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


    var route = _context.Routes.FirstOrDefault(r => r.Name == driver.RouteName);
    
    if (route == null)
        return NotFound(new { message = "Route not found for this driver" });

    // Get all stops for this route in order
    var stops = _context.RouteStations
        .Where(rs => rs.RouteId == route.Id)
        .OrderBy(rs => rs.StopOrder)
        .Select(rs => new {
            stopOrder  = rs.StopOrder,
            pickupTime = rs.PickupTime,
            station = new {
                id   = rs.Station.Id,
                name = rs.Station.Name,
            },
            // Students assigned to this stop
            students = _context.Users
                .Where(u => u.StopName == rs.Station.Name
                     && u.BusNumber == driver.BusNumber
                     && u.Role == "Student")
                .Select(u => new {
                    id            = u.Id,
                    fullName      = u.FullName,
                    grade         = u.Grade ?? "",
                    paymentStatus = _context.Subscriptions
                        .Where(s => s.UserId == u.Id)
                        .OrderByDescending(s => s.Id)
                        .Select(s => s.Status)
                        .FirstOrDefault() ?? "UNPAID",
                })
                .ToList()
        })
        .ToList()
        .Select(rs => new {  // Use client-side LINQ for null handling
            rs.stopOrder,
            rs.pickupTime,
            station = new {
                id   = rs.station.id,
                name = rs.station.name ?? "Unknown",
            },
            rs.students
        })
        .ToList();
    return Ok(new {
        routeName    = route.Name,
        busNumber    = driver.BusNumber ?? "Not assigned",
        totalStops   = stops.Count,
        stops        = stops,
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
        
public IActionResult GetStudentInfo(int userId)
{
    var user = _context.Users
        .FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return NotFound(new { message = "User not found" });

    return Ok(new {
        fullName   = user.FullName,
        uniqueID   = user.UniqueID,
        grade      = user.Grade ?? "",
        busNumber  = "BUS-101",       // ← لسه static لحد ما تعمل Bus table
        driverName = "Michael Davis", // ← لسه static
        routeName  = "Route A - Downtown", // ← static
        stopName   = "Main Street Station" // ← static
    });
}

    }  
}