using diplomaProject.DTOs;

namespace diplomaProject.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardProgressDTO> GetUserStatistic(string userId, int courseId);

        Task <List<int>> GetGrades(string userId);
        Task <DashboardViewDTO> GetDashboardView(string userId, int courseId);
    }
}
