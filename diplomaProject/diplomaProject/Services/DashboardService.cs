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
            _progressService = progressService;
        }

        public async Task<DashboardViewDTO> GetDashboardView(string userId, int courseId)
        {
            var progressData = await GetUserStatistic(userId, courseId);

            var gradesList = await GetGrades(userId);

            var homeworkStats = await GetHomeworkStats(userId);

            var title =await _context.Courses.Where(x => x.Id == courseId).Select(t=>t.Title).FirstOrDefaultAsync();

            var registration = await _context.CourseRegistrations
        .FirstOrDefaultAsync(cr => cr.UserId == userId && cr.CourseId == courseId);

            bool isStarted = await _context.UserProgresses
         .AnyAsync(up => up.UserId == userId && up.CourseId == courseId && up.Status != ProgressStatus.Close);


            

           

            return new DashboardViewDTO
            {
                //Progress= progressData,
                CourseTitle = title ?? "Курс не знайдено",
                CourseId = courseId,
                Progress = progressData,

                Grades = gradesList,
                HomeworkStats = homeworkStats,

                HasStarted = await _context.UserProgresses
                    .AnyAsync(up => up.UserId == userId && up.CourseId == courseId && up.Status != ProgressStatus.Close),
                IsPaid= registration?.IsPaid ?? false,
                FirstModuleCompleted=await _progressService.IsFirstModuleCompletedAsync(userId,courseId)

            };

        }

        public async Task<List<GradeDTO>> GetGrades(string userId)
        {
            return await _context.HomeworkSubmissions
        .Where(g => g.StudentId == userId && g.Status == HomeworkStatus.Approved && g.Grade.HasValue)
        .Select(g => new GradeDTO
        {
            Value = g.Grade.Value,
            Date = g.SubmissionDate 
        })
        .OrderBy(g => g.Date) 
        .ToListAsync();
           
        }
       
        public async Task<HomeworkStatsDTO> GetHomeworkStats(string userId)
        {
            int total =await _context.Lessons.CountAsync();
            int completed = await _context.HomeworkSubmissions
                        .Where(s => s.StudentId == userId && s.Status == HomeworkStatus.Approved)
                      .CountAsync();
            int available = await _context.UserProgresses
                .Where(s => s.UserId == userId && s.Status == ProgressStatus.InProgress && s.LessonId!=null)
                 .CountAsync(); 

            return new HomeworkStatsDTO
            {
                TotalWork = total,
                AvailableWork = available,
                Completed = completed
            };
        }

        public async Task<DashboardProgressDTO> GetUserStatistic(string userId, int courseId)
        {
            var allModules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            var moduleProgress = await _context.UserProgresses
                .Include(m => m.Module)
                .Where(m => m.UserId == userId && m.CourseId == courseId && m.LessonId == null )
                .ToListAsync();

            int total = moduleProgress.Count;
            int completed = moduleProgress.Count(s => s.Status == ProgressStatus.Completed);

            var lessonsProgress = await _context.UserProgresses
                .Include(m => m.Lesson)
                .ThenInclude(l => l.Module)
                .Where(m => m.UserId == userId && m.CourseId == courseId &&  m.LessonId != null)
                .ToListAsync();

            var moduleStats = allModules
    .Select(module =>
    {
        //var moduleLessonsProgress = lessonsProgress.Where(lp => lp.Lesson?.ModuleId == module.Id).ToList();
        var moduleLessonsProgress = lessonsProgress.Where(lp => lp.Lesson?.ModuleId == module.Id).ToList();

        // 1. Визначаємо активну лекцію для ЦЬОГО модуля
        // Шукаємо спочатку ту, що InProgress, якщо немає - першу Open
        var activeLessonInModule = moduleLessonsProgress
            .OrderByDescending(lp => lp.Status == ProgressStatus.InProgress)
            .ThenBy(lp => lp.Status == ProgressStatus.Open)
            .FirstOrDefault();

        // --- НОВА ЛОГІКА ВИЗНАЧЕННЯ СТАТУСУ ---
        ProgressStatus currentModuleStatus;

        if (moduleLessonsProgress.Any() && moduleLessonsProgress.All(lp => lp.Status == ProgressStatus.Completed))
        {
            // Всі лекції пройдені
            currentModuleStatus = ProgressStatus.Completed;
        }
        else if (moduleLessonsProgress.Any(lp => lp.Status == ProgressStatus.InProgress || lp.Status == ProgressStatus.Completed))
        {
            // Хоча б одна лекція в процесі або вже завершена (але не всі)
            currentModuleStatus = ProgressStatus.InProgress;
        }
        else if (moduleLessonsProgress.Any(lp => lp.Status == ProgressStatus.Open))
        {
            // Модуль щойно відкрили (перша лекція доступна)
            currentModuleStatus = ProgressStatus.Open;
        }
        else
        {
            // Жодна лекція не доступна
            currentModuleStatus = ProgressStatus.Close;
        }
        // ---------------------------------------

        return new ModuleProgressDTO
        {
            ModuleId = module.Id,
            Name = module.Title ?? "Модуль",
            ModuleNumber = module.OrderIndex,
            ImageForUser = module.ImageUrl,
            TotalLesson = module.Lessons?.Count ?? 0,
            CompletedLesson = moduleLessonsProgress.Count(g => g.Status == ProgressStatus.Completed),
            Percent = (module.Lessons != null && module.Lessons.Count > 0)
                ? (int)((double)moduleLessonsProgress.Count(g => g.Status == ProgressStatus.Completed) / module.Lessons.Count * 100)
                : 0,
            Status = currentModuleStatus.ToString(), // Використовуємо наш обчислений статус
            //CurrentLessonId = activeLessonInModule?.LessonId
            CurrentLessonId = moduleLessonsProgress
        .FirstOrDefault(lp => lp.Status == ProgressStatus.InProgress || lp.Status == ProgressStatus.Open)
        ?.LessonId
        };
    }).OrderBy(m => m.ModuleNumber).ToList();

            //    var moduleStats = allModules
            //        .Select(module =>
            //        {
            //            var moduleLessonsProgress = lessonsProgress.Where(lp => lp.Lesson?.ModuleId == module.Id).ToList();
            //            var moduleRecord = moduleProgress.FirstOrDefault(mp =>
            //                                                 mp.ModuleId == module.Id && (mp.LessonId == null || mp.LessonId == 0));

            //            var currentModuleStatus = moduleRecord?.Status ?? ProgressStatus.Close;


            //            return new ModuleProgressDTO
            //            {
            //                ModuleId = module.Id,
            //                Name = module.Title ?? "Модуль",
            //                ModuleNumber = module.OrderIndex,
            //                ImageForUser = module.ImageUrl,
            //                // Якщо прогресу немає, буде 0, але модуль залишиться у списку
            //                TotalLesson = module.Lessons?.Count ?? 0,
            //                CompletedLesson = moduleLessonsProgress.Count(g => g.Status == ProgressStatus.Completed),
            //                Percent = (module.Lessons != null && module.Lessons.Count > 0)
            //? (int)((double)moduleLessonsProgress.Count(g => g.Status == ProgressStatus.Completed) / module.Lessons.Count * 100)
            //: 0,
            //                Status = currentModuleStatus switch
            //                {
            //                    ProgressStatus.Completed => "Completed",
            //                    ProgressStatus.InProgress => "InProgress",
            //                    ProgressStatus.Open => "Open",
            //                    _ => "Close"
            //                },
            //            };
            //        }).OrderBy(m => m.ModuleNumber).ToList();

            var curLesson = await _progressService.GetActiveLessonAsync(userId, courseId);
            if (curLesson != null)
            {
                var activeModuleInList = moduleStats.FirstOrDefault(m => m.ModuleId == curLesson.ModuleId);
                if (activeModuleInList != null)
                {
                    // Це той самий ID, який шукає ваша в'юшка
                    activeModuleInList.CurrentLessonId = curLesson.Id;

                    // Додатково переконаємося, що статус дозволяє знайти цей модуль через FirstOrDefault
                    activeModuleInList.Status = "InProgress";
                }
            }

            //string currentTitle = curLesson?.Title ?? "Немає активних лекцій";
            string nextTitle = "Немає активних лекцій";
            if (curLesson != null)
            {

                var nextProgress = await _context.Lessons
                    .Where(up => up.LessonIndex > curLesson.LessonIndex) // Шукаємо ту, що після поточної
            .OrderBy(up => up.LessonIndex)
            .FirstOrDefaultAsync();

                if (nextProgress != null )
                {
                    nextTitle = nextProgress.Title;
                }
            }


            return new DashboardProgressDTO
            {
                CourseId = courseId,
                CurrentLesson = curLesson,
                NextLessonTitle = nextTitle,
                TotalModule = moduleStats.Count,
                CompletedModule = moduleStats.Count(m => m.Status == ProgressStatus.Completed.ToString()),
                ModuleProgress = moduleStats
            };
        }

        public async Task<List<ModuleProgressDTO>> GetModulesWithProgress(string userId, int courseId)
        {
            var modules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            var moduleProgresses = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.CourseId == courseId && p.LessonId == 0)
                .ToListAsync();

            var completedLessons = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.CourseId == courseId && p.Status == ProgressStatus.Completed)
                .ToListAsync();

            return modules.Select(m =>
            {
                var progress = moduleProgresses.FirstOrDefault(p => p.ModuleId == m.Id);
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
                    Status = status.ToString()
                };
            }).ToList();
        }

      
    }
}