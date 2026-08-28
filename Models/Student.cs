using System.ComponentModel.DataAnnotations.Schema;

namespace SafeWayAPI.Models;

public class Student
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ParentId { get; set; }
    public string? Grade { get; set; }
    public int? BusId { get; set; }
    public int? StationId { get; set; }

    public User? User { get; set; }
    public User? Parent { get; set; }
    public Bus? Bus { get; set; }
    public Station? Station { get; set; }
}
