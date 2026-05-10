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

        // GET /api/routechangerequests/stations
        // Returns full station info for admin ManageRoutes page.
        // Output shape: { id, name, address, students, scheduledTime, routes }
        [HttpGet("stations")]
        public async Task<IActionResult> GetStations()
        {
            var list = new List<object>();
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            // NOTE: SQLQuery.sql schema doesn't have address.
            // We'll return name as address for now.
            using var cmd = new NpgsqlCommand(@"
                SELECT
                    s.id,
                    s.name,
                    COALESCE(s.name, '') AS address,
                    COALESCE((
                        SELECT COUNT(*)
                        FROM users u
                        WHERE u.role = 'Student' AND u.stopname = s.name
                    ), 0) AS students,
                    COALESCE((
                        SELECT rs.pickuptime
                        FROM routestations rs
                        WHERE rs.stationid = s.id
                        ORDER BY rs.stoporder ASC
                        LIMIT 1
                    ), '') AS scheduledTime,
                    COALESCE((
                        SELECT string_agg(r.name, ', ' ORDER BY r.name)
                        FROM routestations rs
                        JOIN routes r ON r.id = rs.routeid
                        WHERE rs.stationid = s.id AND r.isactive = true
                    ), '') AS routesCsv
                FROM stations s
                WHERE s.isactive = true
                ORDER BY s.name;
            ", con);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var name = reader.GetString(1);
                var address = reader.GetString(2);
                var students = reader.GetInt32(3);
                var scheduledTime = reader.GetString(4);
                var routesCsv = reader.GetString(5);

                var routes = string.IsNullOrWhiteSpace(routesCsv)
                    ? new List<string>()
                    : routesCsv.Split(',').Select(x => x.Trim()).Where(x => x != string.Empty).ToList();

                list.Add(new
                {
                    id,
                    name,
                    address,
                    students,
                    scheduledTime,
                    routes,
                });
            }

            return Ok(list);
        }


        // ── GET /api/routechangerequests/routes
        // Returns full route info for admin ManageRoutes page.
        // Output shape: { id, name, busId, driver, stops, status }
        [HttpGet("routes")]
        public async Task<IActionResult> GetRoutes()
        {
            var list = new List<object>();
            using var con = new NpgsqlConnection(_conn);
            await con.OpenAsync();

            using var cmd = new NpgsqlCommand(@"
                SELECT
                    r.id,
                    r.name,
                    COALESCE(d.busnumber, '') AS busId,
                    COALESCE(d.drivername, '') AS driver,
                    COALESCE((
                        SELECT COUNT(*)
                        FROM routestations rs
                        WHERE rs.routeid = r.id
                    ), 0) AS stops,
                    CASE
                        WHEN r.isactive = true THEN 'Active'
                        ELSE 'Inactive'
                    END AS status
                FROM routes r
                LEFT JOIN users d
                    ON d.role = 'Driver'
                    AND d.routename = r.name
                WHERE r.isactive = true
                ORDER BY r.name;
            ", con);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    busId = reader.GetString(2),
                    driver = reader.GetString(3),
                    stops = reader.GetInt32(4),
                    status = reader.GetString(5),
                });
            }

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