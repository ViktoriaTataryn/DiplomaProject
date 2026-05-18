using diplomaProject.DTOs;

namespace diplomaProject.Interfaces;

public interface IDashboardService
{
    Task<DashboardProgressDto> GetUserStatistic(string userId, int courseId);

    Task<List<GradeDto>> GetGrades(string userId);
    Task<DashboardViewDto> GetDashboardView(string userId, int courseId);
    Task<HomeworkStatsDto> GetHomeworkStats(string userId);
}