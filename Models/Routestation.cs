namespace SafeWayAPI.Models;

public class RouteStation
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public int StationId { get; set; }
    public int StopOrder { get; set; }
    public TimeSpan PickupTime { get; set; }

    public string PickupTimeFormatted => PickupTime.ToString(@"hh\:mm");

    public BusRoute? Route { get; set; }
    public Station? Station { get; set; }
}