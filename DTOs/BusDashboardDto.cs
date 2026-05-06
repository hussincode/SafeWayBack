namespace SafeWayAPI.DTOs
{
    public class BusDashboardDto
    {
        public string Id { get; set; } = "";
        public string Driver { get; set; } = "";
        public string Route { get; set; } = "";
        public string Occupancy { get; set; } = "";
        public string NextStop { get; set; } = "";
        public string Status { get; set; } = "Active";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Color { get; set; } = "";
    }

    public class ActivityDto
    {
        public string Type { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public string Details { get; set; } = "";
        public string Bus { get; set; } = "";
    }

    public class StatCardDto
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Sub { get; set; } = "";
        public string SubColor { get; set; } = "";
        public string Icon { get; set; } = "";
        public string IconBg { get; set; } = "";
        public string IconColor { get; set; } = "";
        public string BorderColor { get; set; } = "";
    }

    public class AdminDashboardSummaryDto
    {
        public List<StatCardDto> Stats { get; set; } = new();
        public List<BusDashboardDto> Buses { get; set; } = new();
        public List<ActivityDto> Activities { get; set; } = new();
    }
}
