using Microsoft.AspNetCore.Mvc;
using Npgsql;

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
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, name FROM stations WHERE isactive = true ORDER BY name", con);
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
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT id, name FROM routes WHERE isactive = true ORDER BY name", con);
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

            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // Block duplicate PENDING request for same user
            var checkCmd = new NpgsqlCommand(@"
                SELECT COUNT(*) FROM routechangerequests
                WHERE userid = @UserId AND status = 'PENDING'", con);
            checkCmd.Parameters.AddWithValue("@UserId", dto.UserId);
            var existing = (long)(await checkCmd.ExecuteScalarAsync() ?? 0L);
            if (existing > 0)
                return Conflict(new { message = "You already have a pending route change request. Please wait for it to be reviewed." });

            var insertCmd = new NpgsqlCommand(@"
                INSERT INTO routechangerequests
                    (userid, newstationid, newrouteid, effectivedate, status)
                VALUES
                    (@UserId, @NewStationId, @NewRouteId, @EffectiveDate, 'PENDING')
                RETURNING id", con);

            insertCmd.Parameters.AddWithValue("@UserId",        dto.UserId);
            insertCmd.Parameters.AddWithValue("@NewStationId",  dto.NewStationId);
            insertCmd.Parameters.AddWithValue("@NewRouteId",    dto.NewRouteId);
            insertCmd.Parameters.AddWithValue("@EffectiveDate", dto.EffectiveDate);

            var newId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);
            return Ok(new { id = newId, message = "Route change request submitted successfully." });
        }

        // ── GET /api/routechangerequests/user/{userId}
        //    Returns request history for a student
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var list = new List<object>();
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT
                    rcr.id,
                    s.name        AS StationName,
                    r.name        AS RouteName,
                    rcr.effectivedate,
                    rcr.status,
                    rcr.adminnote,
                    rcr.createdat
                FROM routechangerequests rcr
                JOIN stations s ON s.id = rcr.newstationid
                JOIN routes   r ON r.id = rcr.newrouteid
                WHERE rcr.userid = @UserId
                ORDER BY rcr.createdat DESC", con);
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