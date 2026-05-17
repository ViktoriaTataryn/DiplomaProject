namespace diplomaProject.Models;

public class CourseRegistration
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int CourseId { get; set; }
    public DateTime RegisterAt { get; set; } = DateTime.UtcNow;
    public bool IsPaid { get; set; } = false;
    public DateTime? PaymentDate { get; set; }
}