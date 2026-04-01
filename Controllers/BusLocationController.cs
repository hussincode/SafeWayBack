using Microsoft.AspNetCore.Mvc;

namespace SafeWayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusLocationController : ControllerBase
{
    private static double _lat = 24.7136;
    private static double _lng = 46.6753;
    private static DateTime _updatedAt = DateTime.UtcNow;

    [HttpPost("update")]
    public IActionResult UpdateLocation([FromBody] LocationDto dto)
    {
        _lat = dto.Latitude;
        _lng = dto.Longitude;
        _updatedAt = DateTime.UtcNow;
        return Ok(new { message = "Location updated" });
    }
    // Student location storage
private static double _studentLat = 30.0444;
private static double _studentLng = 31.2357;
private static DateTime _studentUpdatedAt = DateTime.UtcNow;

// POST: api/buslocation/student-update
[HttpPost("student-update")]
public IActionResult UpdateStudentLocation([FromBody] LocationDto dto)
{
    _studentLat = dto.Latitude;
    _studentLng = dto.Longitude;
    _studentUpdatedAt = DateTime.UtcNow;
    return Ok(new { message = "Student location updated" });
}

// GET: api/buslocation/student-current
[HttpGet("student-current")]
public IActionResult GetStudentLocation()
{
    return Ok(new {
        latitude  = _studentLat,
        longitude = _studentLng,
        updatedAt = _studentUpdatedAt
    });
}
    [HttpGet("current")]
    public IActionResult GetLocation()
    {
        return Ok(new { latitude = _lat, longitude = _lng, updatedAt = _updatedAt });
    }
}

public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}