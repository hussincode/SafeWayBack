namespace SafeWayAPI.DTOs
{
    public class DriverRecordDto
    {
        public int Id { get; set; }
        public string DriverId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string BusId { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // 'Active' | 'Inactive'
    }
}

