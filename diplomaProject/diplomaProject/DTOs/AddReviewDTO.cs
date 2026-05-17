namespace diplomaProject.DTOs;

public class AddReviewDto
{
    public string Content { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public int Rating { get; set; }
}