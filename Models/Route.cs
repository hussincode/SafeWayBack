namespace SafeWayAPI.Models;
 
public class BusRoute
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public bool   IsActive { get; set; } = true;
}