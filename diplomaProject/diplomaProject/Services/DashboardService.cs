using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;

namespace diplomaProject.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IProgressService _progressService;

        public DashboardService(AppDbContext context, IProgressService progressService)
        {
            _context = context;
            _progressService=progressService;

        }

        public async Task<DashboardViewDTO> GetDashboardView(string userId, int courseId)
        {
             var progressData = await GetUserStatistic(userId, courseId);
            //var modulesList = await GetModulesWithProgress(userId, courseId);
          
            var gradesList =await GetGrades(userId);

            //var progressData = new DashboardProgressDTO
            //{
            //    ModuleProgress = modulesList, // Передаємо наш список сюди
            //    TotalModule = modulesList.Count,
            //    CompletedModule = modulesList.Count(m => m.Percent == 100), // Модуль пройдено, якщо 100%
            //    CurrentLesson = await _progressService.GetActiveLessonAsync(userId, courseId)
            //};

           

            return new DashboardViewDTO
            {
                //Progress= progressData,
                Progress=progressData,
          
                Grades= gradesList,
            }; 
           
        }

        public async Task<List<int>> GetGrades(string userId)
        {
            return  await _context.HomeworkSubmissions
                .Where(g => g.StudentId == userId&&g.Status==HomeworkStatus.Approved)
                .Where(g=>g.Grade.HasValue)
                .Select(g=>g.Grade.Value)
                .ToListAsync();
        }



        public async Task<DashboardProgressDTO> GetUserStatistic(string userId, int courseId)
        {
            var allModules = await _context.Modules
        .Where(m => m.CourseId == courseId)
        .Include(m => m.Lessons)
        .OrderBy(m => m.OrderIndex)
        .ToListAsync();


            var moduleProgress = await _context.UserProgresses
                .Include(m=>m.Module)
                .Where(m => m.UserId == userId && m.CourseId == courseId && (m.LessonId == null || m.LessonId == 0))// m.LessonId == 0 вибираємо тільки модулі
                .ToListAsync();

            int total = moduleProgress.Count;
            int completed = moduleProgress.Count(s => s.Status == ProgressStatus.Completed);

            var lessonsProgress = await _context.UserProgresses
                .Include(m => m.Lesson)
                .ThenInclude(l => l.Module)
                .Where(m => m.UserId == userId && m.CourseId == courseId && m.LessonId != 0 &&
                    m.LessonId != null) // m.LessonId != 0 вибираємо тільки лекції
                .ToListAsync();

           
            var moduleStats = lessonsProgress
                .GroupBy(m => m.ModuleId)
                .Select(group =>
                {
                    var firstItem = group.FirstOrDefault();
                    var module = firstItem?.Lesson?.Module; // Отримуємо посилання на об'єкт модуля
                    var moduleRecord = moduleProgress.FirstOrDefault(mp =>
      mp.ModuleId == group.Key && (mp.LessonId == null || mp.LessonId == 0));
                    var currentModuleStatus = moduleRecord?.Status ?? ProgressStatus.Close;

                    return new ModuleProgressDTO
                    {
                        ModuleId = group.Key.Value,
                        Name = module?.Title ?? "Модуль",
                        ModuleNumber = module?.OrderIndex ?? 0,
                        ImageForUser = module?.ImageUrl,
                        TotalLesson = group.Count(),
                        CompletedLesson = group.Count(g => g.Status == ProgressStatus.Completed),
                        Status = currentModuleStatus switch
                        {
                            ProgressStatus.Completed => "Completed",
                            ProgressStatus.InProgress => "InProgress",
                            ProgressStatus.Open => "Open",
                            _ => "Close"
                        },
                    };

                }).ToList();

            var curLesson = await _progressService.GetActiveLessonAsync(userId, courseId);

            return new DashboardProgressDTO
            {
                CourseId = courseId,
                CurrentLesson = curLesson,
                TotalModule = moduleStats.Count,
                CompletedModule = moduleStats.Count(m => m.Status == ProgressStatus.Completed.ToString()),
                ModuleProgress = moduleStats
            };

        }


        public async Task<List<ModuleProgressDTO>> GetModulesWithProgress(string userId, int courseId)
        {
            // Отримуємо структуру курсу (модулі та їхні лекції)
            var modules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            var moduleProgresses = await _context.UserProgresses
        .Where(p => p.UserId == userId && p.CourseId == courseId && p.LessonId == 0)
        .ToListAsync();

            // Отримуємо тільки завершені лекції користувача
            var completedLessons = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.CourseId == courseId && p.Status == ProgressStatus.Completed)
                .ToListAsync();

            return modules.Select(m =>
            {
                // Шукаємо запис прогресу для конкретного модуля
                var progress = moduleProgresses.FirstOrDefault(p => p.ModuleId == m.Id);

                // Якщо запису немає (новий модуль), за замовчуванням Close
                var status = progress?.Status ?? ProgressStatus.Close;

                return new ModuleProgressDTO
                {
                    ModuleId = m.Id,
                    Name = m.Title,
                    ModuleNumber = m.OrderIndex,
                    ImageForUser = m.ImageUrl,
                    Percent = m.Lessons.Any()
            ? (completedLessons.Count(cp => cp.ModuleId == m.Id) * 100) / m.Lessons.Count()
            : 0,
                    Status = status.ToString() // Тепер статус береться з БД
                };
            }).ToList();

        }
    }
}
