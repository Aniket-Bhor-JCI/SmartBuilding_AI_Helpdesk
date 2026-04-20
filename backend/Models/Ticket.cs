namespace backend.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Issue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
    public string? AssignedTo { get; set; }
}
