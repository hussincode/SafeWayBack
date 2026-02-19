namespace SafeWayAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UniqueID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}