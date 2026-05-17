using diplomaProject.Models;

namespace diplomaProject.DTOs;

public class HomeworkDashboardDto
{
    // 1. Current task info
    public Homework? CurrentHomework { get; set; }

    // 2. Statistics (Your progress)
    public HomeworkStatsDto? Stats { get; set; }
    public double AverageScore { get; set; }

    // 3. Lists for the UI
    public List<HomeworkSubmission>? ExecutedHomeworks { get; set; }
    public List<Module>? AllModules { get; set; }
}