namespace SafeWayAPI.Models;

public class RouteStation
{
    public int    Id         { get; set; }
    public int    RouteId    { get; set; }
    public int    StationId  { get; set; }
    public int    StopOrder  { get; set; }
    public string PickupTime { get; set; } = "";

public BusRoute? Route { get; set; }  
public Station? Station { get; set; }
}