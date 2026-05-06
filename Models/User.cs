namespace SafeWayAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UniqueID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Driver active/inactive (optional)
        public string? Status { get; set; }
        public string? Grade { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentId { get; set; }
        public string? BusNumber  { get; set; }
public string? DriverName { get; set; }
public string? RouteName  { get; set; }
public string? StopName   { get; set; }
    }
}