namespace SafeWayAPI.Models;

public class StationChangeRequest
{
    public int       Id            { get; set; }
    public int       UserId        { get; set; }
    public int       NewStationId  { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string    Status        { get; set; } = "PENDING";
    public DateTime  CreatedAt     { get; set; }

    public User?    User       { get; set; }
    public Station? NewStation { get; set; }
}