using System.ComponentModel.DataAnnotations.Schema;

namespace SafeWayAPI.Models;

public class RouteChangeRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int NewStationId { get; set; }
    public int NewRouteId { get; set; }

    [Column(TypeName = "date")]
    public DateTime EffectiveDate { get; set; }

    public string Status { get; set; } = "PENDING";
    public string? AdminNote { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Station? NewStation { get; set; }
    public BusRoute? NewRoute { get; set; }
}
