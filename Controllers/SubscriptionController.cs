using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeWayAPI.Data;

namespace SafeWayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly AppDbContext _context;
    public SubscriptionController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/subscription/student/2
    // Student sees their own subscription
    [HttpGet("student/{userId}")]
    public async Task<IActionResult> GetByStudent(int userId)
    {
        var sub = await _context.Subscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (sub == null)
            return NotFound(new { message = "No subscription found" });

        // Auto-expire if end date has passed
        if (sub.Status == "PAID" && sub.EndDate < DateTime.Today)
        {
            sub.Status = "EXPIRED";
            await _context.SaveChangesAsync();
        }

        return Ok(new {
            id         = sub.Id,
            status     = sub.Status,
            startDate  = sub.StartDate.ToString("MMMM dd, yyyy"),
            endDate    = sub.EndDate.ToString("MMMM dd, yyyy"),
        });
    }

    // GET: api/subscription/parent/3
    // Parent sees subscriptions of all their children
    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParent(int parentId)
    {
        // Get all students linked to this parent
        var studentIds = await _context.Users
            .Where(u => u.ParentId == parentId)
            .Select(u => u.Id)
            .ToListAsync();

        if (!studentIds.Any())
            return NotFound(new { message = "No children found" });

        var subscriptions = await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => studentIds.Contains(s.UserId))
            .GroupBy(s => s.UserId)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .ToListAsync();

        var result = subscriptions.Select(sub => new {
            studentName = sub.User!.FullName,
            status      = sub.Status,
            startDate   = sub.StartDate.ToString("MMMM dd, yyyy"),
            endDate     = sub.EndDate.ToString("MMMM dd, yyyy"),
        });

        return Ok(result);
    }

    // PUT: api/subscription/update/1
    // Admin updates subscription status
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var sub = await _context.Subscriptions.FindAsync(id);
        if (sub == null) return NotFound();

        sub.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated successfully" });
    }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = "";
}