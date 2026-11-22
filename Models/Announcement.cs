namespace SafeWayAPI.Models
{
    public class Announcement
    {
        public int      Id          { get; set; }
        public int      DriverId    { get; set; }
        public string   BusNumber   { get; set; } = string.Empty;
        public string   Title       { get; set; } = string.Empty;
        public string   Message     { get; set; } = string.Empty;
        public string   Type        { get; set; } = "General"; // General | Delay | Emergency | Schedule
        public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
        public bool     IsRead      { get; set; } = false;

        // Navigation
        public User? Driver { get; set; }
    }
}
