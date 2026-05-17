namespace diplomaProject.DTOs;

public class DashboardViewDto
{
    public string? CourseTitle { get; set; }
    public int CourseId { get; set; }
    public DashboardProgressDto? Progress { get; set; }
    public UserProfileDto? UserProfile { get; set; }

    public List<GradeDto> Grades { get; set; } = [];
    public HomeworkStatsDto? HomeworkStats { get; set; }

    public bool HasStarted { get; set; } // Чи розпочав користувач хоча б одну лекцію
    public bool FirstModuleCompleted { get; set; } // Чи пройдено 1-й модуль

    public bool IsPaid { get; set; }
    //public string NextLessonTitle { get; set; }
}