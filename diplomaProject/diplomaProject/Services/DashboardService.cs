using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;

        public DashboardService(AppDbContext context, ProgressService progressService)
        {
            _context = context;
            _progressService=progressService;

        }

        public async Task<UserProfileDTO> GetUserProfile(string userId)
        {
            var userData = await _context.Users.Where(u => u.Id == userId)
                .Select(u=>new UserProfileDTO
                {
                    UserName = u.FirstName,
                    UserLastName = u.LastName,
                    UserEmail = u.Email,
                    UserPhone = u.PhoneNumber
                })
                .FirstOrDefaultAsync() ;

            if (userData == null)
            {
                throw new KeyNotFoundException($"Користувач з ID {userId} не знайдений."); ;
            }

            return userData;
        }

        public async Task<DashboardProgressDTO> GetUserStatistic(string userId, int courseId)
        {
            var moduleProgress = await _context.UserProgresses
                .Where(m => m.UserId == userId && m.CourseId == courseId && m.LessonId == 0)// m.LessonId == 0 вибираємо тільки модулі
                .ToListAsync();
            int total = moduleProgress.Count;
            int completed = moduleProgress.Count(s => s.Status == ProgressStatus.Completed);

            var lessonsProgress = await _context.UserProgresses
                .Include(m => m.Lesson)
                .ThenInclude(l => l.Module)
                .Where(m => m.UserId == userId && m.CourseId == courseId && m.LessonId != 0) // m.LessonId != 0 вибираємо тільки лекції
                .ToListAsync();

            var moduleStats = lessonsProgress
                .GroupBy(m => m.ModuleId)
                .Select(group => new ModuleProgressDTO
                {
                    ModuleId = group.Key,
                    Name = group.FirstOrDefault()?.Lesson.Module.Title ?? "Модуль",
                    ModuleNumber = group.FirstOrDefault()?.Lesson.Module.OrderIndex ?? 0,
                    TotalLesson = group.Count(),
                    CompletedLesson = group.Count(g => g.Status == ProgressStatus.Completed),

                }).ToList();

            var curLesson = await _progressService.GetActiveLessonAsync(userId, courseId);

            return new DashboardProgressDTO
            {
                CurrentLesson = curLesson,
                TotalModule = total,
                CompletedModule = completed,
                ModuleProgress = moduleStats
            };

        }
    }
}
