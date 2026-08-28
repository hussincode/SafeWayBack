using Microsoft.AspNetCore.Mvc;
using SafeWayAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace SafeWayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            var list = new List<object>();

            //  Station / Route Change Requests Notifications
            var stationRequests = await _context.StationChangeRequests
                .Include(r => r.NewStation)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            foreach (var req in stationRequests)
            {
                string statusText = req.Status.ToUpper();
                string title = $"Station Change Request: {statusText}";
                string subtitle = $"Requested station: {req.NewStation?.Name ?? "Station"}";
                string type = req.Status == "APPROVED" ? "success" : (req.Status == "REJECTED" ? "error" : "info");

                list.Add(new
                {
                    id = $"req_{req.Id}",
                    title = title,
                    subtitle = subtitle,
                    status = req.Status,
                    type = type,
                    createdAt = req.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    time = req.CreatedAt.ToString("MMM dd, HH:mm"),
                    isRead = req.Status != "PENDING"
                });
            }

            // 2. Subscription Status Notifications
            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                list.Add(new
                {
                    id = $"sub_{subscription.Id}",
                    title = $"Subscription Status: {subscription.Status}",
                    subtitle = $"Valid from {subscription.StartDate:MMM dd} to {subscription.EndDate:MMM dd, yyyy}",
                    status = subscription.Status,
                    type = subscription.Status == "ACTIVE" ? "success" : "warning",
                    createdAt = subscription.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    time = subscription.CreatedAt.ToString("MMM dd, HH:mm"),
                    isRead = true
                });
            }

            // 3. Live Bus / Schedule Notification (Dynamic context based on user assigned route/bus)
            if (!string.IsNullOrEmpty(user.BusNumber) && user.BusNumber != "Not assigned")
            {
                list.Add(new
                {
                    id = "bus_live_1",
                    title = $"Bus {user.BusNumber} is active on schedule",
                    subtitle = $"Route: {user.RouteName ?? "Morning Route"} · Stop: {user.StopName ?? "Your stop"}",
                    status = "INFO",
                    type = "bus",
                    createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                    time = "Live update",
                    isRead = false
                });
            }

            return Ok(list);
        }
    }
}
