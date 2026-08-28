namespace SafeWayAPI.Models;

public class Bus
{
    public int Id { get; set; }
    public string BusNumber { get; set; } = "";
    public int? DriverId { get; set; }
    public int? RouteId { get; set; }
    public bool IsActive { get; set; } = true;

    public User? Driver { get; set; }
    public BusRoute? Route { get; set; }
}
