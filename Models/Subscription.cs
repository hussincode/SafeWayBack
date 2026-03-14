namespace SafeWayAPI.Models;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "UNPAID";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}