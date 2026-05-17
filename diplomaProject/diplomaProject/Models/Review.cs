namespace diplomaProject.Models;

public class Review
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public int Rating { get; set; }
}