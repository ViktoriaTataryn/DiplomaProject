using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace diplomaProject.Services
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _context;

        public ProgressService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Lesson> GetActiveLessonAsync(string userId, int courseId)
        {
            var activeLesson = await _context.UserProgresses
                .Include(l => l.Lesson)
                .ThenInclude(m => m.Module)
                .Where(u => u.UserId == userId && u.CourseId == courseId && u.LessonId != 0)
                .OrderByDescending(a => a.LastActivity)
                .FirstOrDefaultAsync(s => s.Status == ProgressStatus.InProgress || s.Status == ProgressStatus.Open);

            //if (activeLesson == null) { 
            //    var openLesson = await _context.UserProgresses
            //    .Include(l => l.Lesson)
            //    .ThenInclude(m => m.Module)
            //    .Where(u => u.UserId == userId && u.CourseId == courseId && u.LessonId != 0)
            //    .OrderBy(l=>l.LessonId)
            //    .FirstOrDefaultAsync(s => s.Status == ProgressStatus.Open);
            //    return openLesson.Lesson.Title;
            //}
            if (activeLesson == null || activeLesson.Lesson == null)
            {
                // Вместо ошибки возвращаем заглушку, чтобы Dashboard не падал
                return "Уроків ще немає";
            }
            return activeLesson.Lesson;

            return activeLesson.Lesson.Title;
        }

        public async Task<HomeworkStatus> GetHomeworkStatusAsync(string userId, int homeworkId)
        {
            var status = await _context.HomeworkSubmissions.FirstOrDefaultAsync(s => s.StudentId == userId && s.HomeworkId == homeworkId);
            if (status == null)
            {
                return HomeworkStatus.NotSubmitted;
            }
            return status.Status;
        }

        public async Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId)
        {
            var status = await _context.UserProgresses.FirstOrDefaultAsync(s => s.UserId == userId && s.LessonId == lessonId && s.LessonId != 0);
            if (status == null)
            {
                return ProgressStatus.Close;
            }
            return status.Status;
        }

        public async Task StartCourse(string userId, int courseId)
        {
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.Id)
                .ToListAsync();

            if (!modules.Any()) return;

            var progressEntries = new List<UserProgress>();
            bool isFirstModule = true;

            foreach (var module in modules)
            {
                progressEntries.Add(new UserProgress
                {
                    UserId = userId,
                    CourseId = courseId,
                    ModuleId = module.Id,
                    LessonId = null,
                    Status = isFirstModule ? ProgressStatus.Open : ProgressStatus.Close,
                    LastActivity = DateTime.Now
                });

                bool isFirstLessonInCourse = isFirstModule;
                bool isFirstLessonInModule = true;

                foreach (var lesson in module.Lessons.OrderBy(l => l.Id))
                {
                    progressEntries.Add(new UserProgress
                    {
                        UserId = userId,
                        CourseId = courseId,
                        ModuleId = module.Id,
                        LessonId = lesson.Id,
                        Status = (isFirstLessonInCourse && isFirstLessonInModule)
                                     ? ProgressStatus.Open
                                     : ProgressStatus.Close,
                        LastActivity = DateTime.Now
                    });
                    isFirstLessonInModule = false;
                }
                isFirstModule = false;
            }

            _context.UserProgresses.AddRange(progressEntries);
            await _context.SaveChangesAsync();
        }

        public async Task OpenLessonAsync(string userId, int lessonId)
        {
            var lessonProgress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId && p.LessonId != 0);
            if (lessonProgress != null && lessonProgress.Status == ProgressStatus.Close)
            {
                lessonProgress.Status = ProgressStatus.Open;
            }
            await _context.SaveChangesAsync();
        }

        public async Task LessonInProgressAsync(string userId, int lessonId)
        {
            var lessonProgress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId && p.LessonId != 0);
            if (lessonProgress != null && lessonProgress.Status == ProgressStatus.Open)
            {
                lessonProgress.Status = ProgressStatus.InProgress;
            }

            if (lessonProgress != null)
            {
                var moduleProgress = await _context.UserProgresses.FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == lessonProgress.ModuleId);
                if (moduleProgress != null && moduleProgress.Status == ProgressStatus.Open)
                {
                    moduleProgress.Status = ProgressStatus.InProgress;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task UnlockNextLessonAsync(string userId, int currentLessonId)
        {
            var lessonProgress = await _context.UserProgresses.FirstOrDefaultAsync(l => l.UserId == userId && l.LessonId == currentLessonId && l.LessonId != 0);
            var homeworkStatus = await _context.HomeworkSubmissions
                .Include(h => h.Homework)
                .FirstOrDefaultAsync(h => h.StudentId == userId && h.Homework.LessonId == currentLessonId);

            if (lessonProgress != null && homeworkStatus != null && homeworkStatus.Status == HomeworkStatus.Approved)
            {
                lessonProgress.Status = ProgressStatus.Completed;
                lessonProgress.IsCompleted = true;
            }

            }
            var nextLesson = await _context.Lessons.Where(l => l.ModuleId == lessonProgress.ModuleId && l.Id > currentLessonId)
                .OrderBy(l => l.LessonIndex)
                .FirstOrDefaultAsync();
            if (nextLesson != null)
            {
                //var nextLessonStatus = await _context.UserProgresses.FirstOrDefaultAsync(l => l.UserId == userId && l.LessonId == nextLesson.Id);
                //if (nextLessonStatus != null && nextLessonStatus.Status == ProgressStatus.Close) { 
                //    nextLessonStatus .Status=ProgressStatus.Open;
                //}
                
                
                    await OpenLessonAsync(userId, nextLesson.Id);
                }
            }
            await _context.SaveChangesAsync();
        }



        public async Task UnlockNextModuleAsync(string userId, int currentModuleId)
        {
            var allLessonsOfModule = _context.UserProgresses
                        .Where(p => p.UserId == userId && p.ModuleId == currentModuleId && p.LessonId != 0);

            bool isAllCompleted = await allLessonsOfModule.AnyAsync() &&
                         await allLessonsOfModule.AllAsync(p => p.Status == ProgressStatus.Completed);


            if (isAllCompleted)
            {
                // 1. Позначаємо поточний модуль як завершений
                var currentModuleProgress = await _context.UserProgresses
                        .Include(p => p.Module)
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == currentModuleId && m.LessonId == 0);

                if (currentModuleProgress != null)
                {
                    currentModuleProgress.Status = ProgressStatus.Completed;
                    currentModuleProgress.IsCompleted = true;

                    // 2. Шукаємо наступний модуль за порядковим номером
                    var nextModule = await _context.Modules
                        .Where(m => m.OrderIndex > currentModuleProgress.Module.OrderIndex)
                        .OrderBy(m => m.OrderIndex)
                        .FirstOrDefaultAsync();

                    if (nextModule != null)
                    {
                        // 3. Відкриваємо прогрес для самого модуля
                        var nextModuleProgress = await _context.UserProgresses
                            .FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == nextModule.Id && m.LessonId == 0);

                        if (nextModuleProgress != null)
                        {
                            nextModuleProgress.Status = ProgressStatus.InProgress;
                        }

                        // 4. Знаходимо ПЕРШУ лекцію наступного модуля і робимо її InProgress
                        var firstLessonOfNextModule = await _context.UserProgresses
                            .Where(p => p.UserId == userId && p.ModuleId == nextModule.Id && p.LessonId != 0)
                            .OrderBy(p => p.LessonId) 
                            .FirstOrDefaultAsync();

                        if (firstLessonOfNextModule != null)
                        {
                            firstLessonOfNextModule.Status = ProgressStatus.InProgress;
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<bool> IsFirstModuleCompletedAsync(string userId, int courseId)
        {
            var firstModuleId = await _context.Modules
        .Where(m => m.CourseId == courseId)
        .OrderBy(m => m.OrderIndex) 
        .Select(m => m.Id)
        .FirstOrDefaultAsync();

            if (firstModuleId == 0) return false;

            var moduleLessons = await _context.Lessons
                .Where(l => l.ModuleId == firstModuleId)
                .Select(l => l.Id)
                .ToListAsync();

            var progressQuery = _context.UserProgresses
         .Where(p => p.UserId == userId && p.ModuleId == firstModuleId && p.LessonId != 0);

            return await progressQuery.AnyAsync() &&
                   await progressQuery.AllAsync(p => p.Status == ProgressStatus.Completed);
        }
    
    }
}