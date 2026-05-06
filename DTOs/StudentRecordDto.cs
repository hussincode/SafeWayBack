namespace SafeWayAPI.DTOs
{
    public class StudentRecordDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UniqueID { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = "UNPAID";
    }
}

