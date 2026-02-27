using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace diplomaProject.Services
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _context;

        public ProgressService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetActiveLessonAsync(string userId, int courseId)
        {
            var activeLesson = await _context.UserProgresses
                .Include(l=>l.Lesson)
                .ThenInclude(m=>m.Module)
                .Where(u=>u.UserId==userId&&u.CourseId==courseId&&u.LessonId!=0)
                .OrderByDescending(a=>a.LastActivity)
                .FirstOrDefaultAsync(s=>s.Status==ProgressStatus.InProgress);

            if (activeLesson == null) { 
                var openLesson = await _context.UserProgresses
                .Include(l => l.Lesson)
                .ThenInclude(m => m.Module)
                .Where(u => u.UserId == userId && u.CourseId == courseId && u.LessonId != 0)
                .OrderBy(l=>l.LessonId)
                .FirstOrDefaultAsync(s => s.Status == ProgressStatus.Open);
                return openLesson.Lesson.Title;
            }
            return activeLesson.Lesson.Title;

        }

        public async Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId)
        {
            var status= await _context.UserProgresses.FirstOrDefaultAsync(s=>s.UserId == userId&&s.LessonId==lessonId && s.LessonId != 0);
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
                .Where(m => m.CourseId == courseId )
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
                    LessonId = 0, 
                    Status = isFirstModule ? ProgressStatus.Open : ProgressStatus.Close,
                    LastActivity = DateTime.Now
                });

                bool isFirstLessonInCourse = isFirstModule;
                bool isFirstLessonInModule = true;

                foreach (var lesson in module.Lessons.OrderBy(l => l.Id))
                {
                    // Створюємо запис для ЛЕКЦІЇ
                    progressEntries.Add(new UserProgress
                    {
                        UserId = userId,
                        CourseId = courseId,
                        ModuleId = module.Id,
                        LessonId = lesson.Id,
                        // Відкриваємо тільки ПЕРШУ лекцію ПЕРШОГО модуля
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

        public async Task StartLessonAsync(string userId, int lessonId)
        {
           
            var lessonProgress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId && p.LessonId != 0);
            if (lessonProgress != null && lessonProgress.Status==ProgressStatus.Open) {
                lessonProgress.Status = ProgressStatus.InProgress;
               
            }
            var moduleProgress = await _context.UserProgresses.FirstOrDefaultAsync(m=>m.UserId==userId&&m.ModuleId==lessonProgress.ModuleId );
            if (moduleProgress !=null && moduleProgress.Status == ProgressStatus.Open) { 
                moduleProgress.Status = ProgressStatus.InProgress;
            }
            await _context.SaveChangesAsync();
             
        }

        public async Task UnlockNextLessonAsync(string userId, int currentLessonId)
        {
            //homework status=aproved
            //lesson status = completed
            //next lesson = in progress
            var lessonProgress = await _context.UserProgresses.FirstOrDefaultAsync(l => l.UserId == userId && l.LessonId == currentLessonId && l.LessonId != 0);
            var homeworkStatus = await _context.HomeworkSubmissions
                .Include(h => h.Homework)
                .FirstOrDefaultAsync(h => h.StudentId == userId && h.Homework.LessonId == currentLessonId);
            if (homeworkStatus != null && homeworkStatus.Status == HomeworkStatus.Approved) {
                lessonProgress.Status=ProgressStatus.Completed;
                lessonProgress.IsCompleted = true;

            }
            var nextLesson = await _context.Lessons.Where(l=>l.ModuleId== lessonProgress.ModuleId&&l.Id>currentLessonId)
                .OrderBy(l=>l.Id)
                .FirstOrDefaultAsync();
            if (nextLesson != null)
            {
                var nextLessonStatus = await _context.UserProgresses.FirstOrDefaultAsync(l => l.UserId == userId && l.LessonId == nextLesson.Id);
                if (nextLessonStatus != null && nextLessonStatus.Status == ProgressStatus.Close) { 
                    nextLessonStatus .Status=ProgressStatus.Open;
                }
                else
                {
                  await  UnlockNextModuleAsync(userId, lessonProgress.Id);
                }
            }
            await _context.SaveChangesAsync();  
        }

        public async Task UnlockNextModuleAsync(string userId, int currentModuleId)
        {
            var allLessonsOfModule = _context.UserProgresses
                    .Where(p => p.UserId == userId && p.ModuleId == currentModuleId && p.LessonId != 0);
            bool isAllCompleted = await allLessonsOfModule.AllAsync(p => p.Status == ProgressStatus.Completed);
            if (isAllCompleted)
            {
                var module = await _context.UserProgresses
                    .Include(p => p.Module)
                    .FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == currentModuleId && m.LessonId == 0);
                if (module != null)
                {
                    module.Status = ProgressStatus.Completed;
                    module.IsCompleted = true;

                    var nextModule = await _context.Modules.Where(m => m.OrderIndex > module.Module.OrderIndex)
                        .OrderBy(m => m.OrderIndex)
                        .FirstOrDefaultAsync();
                    if (nextModule != null)
                    {
                        var nextModuleProgress = await _context.UserProgresses.FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == currentModuleId && m.LessonId == 0);
                        if (nextModuleProgress != null) {
                            nextModuleProgress.Status = ProgressStatus.Open;
                        }
                    }

                    await _context.SaveChangesAsync();
                }

            }

        }

       
    }
}
