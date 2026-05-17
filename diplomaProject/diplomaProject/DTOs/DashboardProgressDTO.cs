using diplomaProject.Models;

namespace diplomaProject.DTOs;

public class DashboardProgressDto
{
    public int CourseId { get; set; }
    public Lesson? CurrentLesson { get; set; }
    public int CompletedModule { get; set; }
    public int TotalModule { get; set; }
    public List<ModuleProgressDto> ModuleProgress { get; set; } = [];
    public string? NextLessonTitle { get; set; }
}