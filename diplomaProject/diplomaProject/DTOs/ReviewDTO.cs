namespace diplomaProject.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public required string UserName { get; set; }
}