using diplomaProject.Data;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Services
{
    public class ProgressService : IProgressService
    {
        private readonly AppDbContext _context;

        public ProgressService(AppDbContext context)
        {
            _context = context;
        }

        public Task CompleteLessonAndUnlockNextAsync(string userId, int currentLessonId)
        {
            throw new NotImplementedException();
        }

        public Task CompleteLessonAsync(string userId, int lessonId)
        {
            throw new NotImplementedException();
        }

        public Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId)
        {
            throw new NotImplementedException();
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
            var progress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);
            if (progress != null && progress.Status==ProgressStatus.Close) {
                progress.Status = ProgressStatus.InProgress;
                await _context.SaveChangesAsync();
            }
            
            
        }

        public Task UnlockNextLessonAsync(string userId, int currentLessonId)
        {
            throw new NotImplementedException();
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
                        var newtModuleProgress = await _context.UserProgresses.FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == currentModuleId && m.LessonId == 0);
                        if (newtModuleProgress != null) {
                            newtModuleProgress.Status = ProgressStatus.Open;
                        }
                    }

                    await _context.SaveChangesAsync();
                }

            }

        }
        
    }
}
