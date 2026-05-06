namespace SafeWayAPI.DTOs
{
    public class AddDriverRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
    }
}

