using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Data;
using SafeWayAPI.Models;

namespace SafeWayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationController : ControllerBase
{
    private readonly AppDbContext _context;
    public StationController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/station/list
    // Returns all active stations for the dropdown
    [HttpGet("list")]
    public async Task<IActionResult> GetStations()
    {
        var stations = await _context.Stations
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        return Ok(stations);
    }

    // POST: api/station/request
    // Student submits a change request
    [HttpPost("request")]
    public async Task<IActionResult> RequestChange([FromBody] StationRequestDto dto)
    {
        // Check user exists
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Check station exists
        var station = await _context.Stations.FindAsync(dto.NewStationId);
        if (station == null)
            return NotFound(new { message = "Station not found" });

        // Check no pending request already exists
        var existing = await _context.StationChangeRequests
            .AnyAsync(r => r.UserId == dto.UserId && r.Status == "PENDING");

        if (existing)
            return BadRequest(new { message = "You already have a pending request." });

        var request = new StationChangeRequest
        {
            UserId        = dto.UserId,
            NewStationId  = dto.NewStationId,
            EffectiveDate = dto.EffectiveDate,
            Status        = "PENDING",
        };

        _context.StationChangeRequests.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Request submitted successfully. Awaiting admin approval." });
    }

    // GET: api/station/requests/{userId}
    // Get all requests for a student
    [HttpGet("requests/{userId}")]
    public async Task<IActionResult> GetMyRequests(int userId)
    {
        var requests = await _context.StationChangeRequests
            .Include(r => r.NewStation)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                r.Id,
                newStation    = r.NewStation!.Name,
                effectiveDate = r.EffectiveDate.HasValue
                    ? r.EffectiveDate.Value.ToString("MMMM dd, yyyy")
                    : "Not specified",
                r.Status,
                createdAt = r.CreatedAt.ToString("MMMM dd, yyyy"),
            })
            .ToListAsync();

        return Ok(requests);
    }

    // PUT: api/station/approve/{id}
    // Admin approves or rejects a request
    [HttpPut("approve/{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ApproveDto dto)
    {
        var request = await _context.StationChangeRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = dto.Status; // APPROVED or REJECTED
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Request {dto.Status} successfully." });
    }
}

public class StationRequestDto
{
    public int      UserId        { get; set; }
    public int      NewStationId  { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class ApproveDto
{
    public string Status { get; set; } = "";
}