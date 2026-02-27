using diplomaProject.DTOs;

namespace diplomaProject.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardProgressDTO> GetUserStatistic(string userId, int courseId);
        Task<UserProfileDTO> GetUserProfile(string userId);
    }
}
