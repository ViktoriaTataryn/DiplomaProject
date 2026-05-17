namespace diplomaProject.DTOs;

public class LessonDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int ModuleIndex { get; set; }
    public int UserCompletedNum { get; set; }
}