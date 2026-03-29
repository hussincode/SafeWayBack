using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SafeWay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteChangeRequestsController : ControllerBase
    {
        private readonly string _conn;

        public RouteChangeRequestsController(IConfiguration config)
        {
            _conn = config.GetConnectionString("DefaultConnection")!;
        }

        // ── GET /api/routechangerequests/stations
        [HttpGet("stations")]
        public async Task<IActionResult> GetStations()
        {
            var list = new List<object>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM Stations WHERE IsActive = 1 ORDER BY Name", con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
            return Ok(list);
        }

        // ── GET /api/routechangerequests/routes
        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var list = new List<object>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT Id, Name FROM Routes WHERE IsActive = 1 ORDER BY Name", con);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1) });
            return Ok(list);
        }

        // ── POST /api/routechangerequests
        //    Body: { userId, newStationId, newRouteId, effectiveDate }
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] RouteChangeRequestDto dto)
        {
            if (dto.UserId == 0 || dto.NewStationId == 0 || dto.NewRouteId == 0 || dto.EffectiveDate == default)
                return BadRequest(new { message = "userId, newStationId, newRouteId and effectiveDate are all required." });

            using var con = new SqlConnection(_conn);
            await con.OpenAsync();

            // Block duplicate PENDING request for same user
            var checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM RouteChangeRequests
                WHERE UserId = @UserId AND Status = 'PENDING'", con);
            checkCmd.Parameters.AddWithValue("@UserId", dto.UserId);
            var existing = (int)await checkCmd.ExecuteScalarAsync()!;
            if (existing > 0)
                return Conflict(new { message = "You already have a pending route change request. Please wait for it to be reviewed." });

            var insertCmd = new SqlCommand(@"
                INSERT INTO RouteChangeRequests
                    (UserId, NewStationId, NewRouteId, EffectiveDate, Status)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @NewStationId, @NewRouteId, @EffectiveDate, 'PENDING')", con);

            insertCmd.Parameters.AddWithValue("@UserId",        dto.UserId);
            insertCmd.Parameters.AddWithValue("@NewStationId",  dto.NewStationId);
            insertCmd.Parameters.AddWithValue("@NewRouteId",    dto.NewRouteId);
            insertCmd.Parameters.AddWithValue("@EffectiveDate", dto.EffectiveDate.ToString("yyyy-MM-dd"));

            var newId = (int)await insertCmd.ExecuteScalarAsync()!;
            return Ok(new { id = newId, message = "Route change request submitted successfully." });
        }

        // ── GET /api/routechangerequests/user/{userId}
        //    Returns request history for a student
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var list = new List<object>();
            using var con = new SqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT
                    rcr.Id,
                    s.Name        AS StationName,
                    r.Name        AS RouteName,
                    rcr.EffectiveDate,
                    rcr.Status,
                    rcr.AdminNote,
                    rcr.CreatedAt
                FROM RouteChangeRequests rcr
                JOIN Stations s ON s.Id = rcr.NewStationId
                JOIN Routes   r ON r.Id = rcr.NewRouteId
                WHERE rcr.UserId = @UserId
                ORDER BY rcr.CreatedAt DESC", con);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id            = reader.GetInt32(0),
                    stationName   = reader.GetString(1),
                    routeName     = reader.GetString(2),
                    effectiveDate = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                    status        = reader.GetString(4),
                    adminNote     = reader.IsDBNull(5) ? null : reader.GetString(5),
                    createdAt     = reader.GetDateTime(6).ToString("yyyy-MM-dd"),
                });
            }
            return Ok(list);
        }
    }

    public class RouteChangeRequestDto
    {
        public int      UserId        { get; set; }
        public int      NewStationId  { get; set; }
        public int      NewRouteId    { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}