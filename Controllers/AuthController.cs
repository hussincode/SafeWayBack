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

            // BCrypt.Net may throw SaltParseException if the stored hash uses a version
            // prefix that it doesn't recognize (e.g. $2y$ from some PHP implementations).
            // Normalize it to a supported prefix before verifying.
            var storedHash = NormalizeBcryptHash(user.Password);
            bool isCorrect = BCrypt.Net.BCrypt.Verify(request.Password, storedHash);

            if (!isCorrect)
                return Unauthorized(new { message = "Wrong password." });

            var token = GenerateToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role,
                UniqueID = user.UniqueID
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
    var user = _context.Users.FirstOrDefault(u => u.Id == userId);
    if (user == null) return NotFound();

    return Ok(new {
        fullName   = user.FullName,
        uniqueID   = user.UniqueID,
        grade      = user.Grade,
        busNumber  = user.BusNumber  ?? "Not assigned",
        driverName = user.DriverName ?? "Not assigned",
        routeName  = user.RouteName  ?? "Not assigned",
        stopName   = user.StopName   ?? "Not assigned",
    });
}

    }  
}      