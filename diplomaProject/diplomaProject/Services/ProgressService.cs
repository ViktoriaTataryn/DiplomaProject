using diplomaProject.Data;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Services;

public class ProgressService(AppDbContext context) : IProgressService
{
   
    public async Task<Lesson?> GetActiveLessonAsync(string userId, int courseId)
    {
        // Шукаємо запис, де LessonId НЕ NULL (SQL: IS NOT NULL)
        var progress = await context.UserProgresses
            .Where(u => u.UserId == userId && u.CourseId == courseId)
            .Where(u => u.LessonId.HasValue) // Явно кажемо, що значення має бути
            .OrderByDescending(u => u.Status == ProgressStatus.InProgress)
            .ThenByDescending(u => u.LastActivity)
            .FirstOrDefaultAsync();

        if (progress == null) return null;

        // Окремо тягнемо лекцію, щоб уникнути проблем із вкладеними Include
        return await context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == progress.LessonId);
    }


    public async Task<HomeworkStatus> GetHomeworkStatusAsync(string userId, int homeworkId)
    {
        var status =
            await context.HomeworkSubmissions.FirstOrDefaultAsync(s =>
                s.StudentId == userId && s.HomeworkId == homeworkId);
        if (status == null) return HomeworkStatus.NotSubmitted;
        return status.Status;
    }

    public async Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId)
    {
        var status = await context.UserProgresses.FirstOrDefaultAsync(s =>
            s.UserId == userId && s.LessonId == lessonId && s.LessonId != null);
        if (status == null) return ProgressStatus.Close;
        return status.Status;
    }


    public async Task StartCourse(string userId, int courseId)
    {
        // 1. ПЕРЕВІРКА: Якщо прогрес вже існує, просто виходимо
        var hasProgress = await context.UserProgresses
            .AnyAsync(u => u.UserId == userId && u.CourseId == courseId);

        if (hasProgress) return;

       
        var modules = await context.Modules
            .Include(m => m.Lessons)
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        if (!modules.Any()) return;

        var progressEntries = new List<UserProgress>();
        var isFirstModule = true;

        foreach (var module in modules)
        {
            //Створюємо запис для МОДУЛЯ(LessonId = 0)
            progressEntries.Add(new UserProgress
            {
                UserId = userId,
                CourseId = courseId,
                ModuleId = module.Id,
                LessonId = null,
                Status = isFirstModule ? ProgressStatus.InProgress : ProgressStatus.Close,
                LastActivity = DateTime.Now
            });

            var isFirstLesson = true;
            if (module.Lessons != null)
                foreach (var lesson in module.Lessons.OrderBy(l => l.LessonIndex))
                {
                    // Створюємо запис для ЛЕКЦІЇ
                    progressEntries.Add(new UserProgress
                    {
                        UserId = userId,
                        CourseId = courseId,
                        ModuleId = module.Id,
                        LessonId = lesson.Id,
                        Status = isFirstModule && isFirstLesson ? ProgressStatus.Open : ProgressStatus.Close,
                        LastActivity = DateTime.Now
                    });
                    isFirstLesson = false;
                }

            isFirstModule = false;
        }

        context.UserProgresses.AddRange(progressEntries);
        await context.SaveChangesAsync();
    }

    public async Task OpenLessonAsync(string userId, int lessonId)
    {
        
        var progress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);

        if (progress != null)
        {
            if (progress.Status == ProgressStatus.Close) progress.Status = ProgressStatus.Open;
            progress.LastActivity = DateTime.Now;
        }
        else
        {
            // Якщо запису немає — створюємо його зі статусом Open
            var lesson = await context.Lessons.FindAsync(lessonId);
            if (lesson != null)
                context.UserProgresses.Add(new UserProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    ModuleId = lesson.ModuleId,
                    Status = ProgressStatus.Open
                });
        }

        await context.SaveChangesAsync();
    }

    public async Task LessonInProgressAsync(string userId, int lessonId)
    {
        var lessonProgress = await context.UserProgresses.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.LessonId == lessonId && p.LessonId != null);
        if (lessonProgress != null && lessonProgress.Status == ProgressStatus.Open)
        {
            lessonProgress.Status = ProgressStatus.InProgress;
            lessonProgress.LastActivity = DateTime.Now;
        }

        if (lessonProgress != null)
        {
            var moduleProgress =
                await context.UserProgresses.FirstOrDefaultAsync(m =>
                    m.UserId == userId && m.ModuleId == lessonProgress.ModuleId);
            if (moduleProgress != null)
            {
                if (moduleProgress.Status == ProgressStatus.Open) moduleProgress.Status = ProgressStatus.InProgress;
                moduleProgress.LastActivity = DateTime.Now;
            }
        }

        await context.SaveChangesAsync();
    }



    public async Task UnlockNextLessonAsync(string userId, int currentLessonId)
    {
        // 1. Знаходимо поточну лекцію
        var currentLesson =
            await context.Lessons.Include(l => l.Module).FirstOrDefaultAsync(l => l.Id == currentLessonId);
        if (currentLesson == null) return;

        // 2. Оновлюємо статус поточної лекції
        var currentProgress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == currentLessonId);

        if (currentProgress != null)
        {
            currentProgress.Status = ProgressStatus.Completed;
            await context.SaveChangesAsync();
        }

      
        var nextLesson = await context.Lessons
            .Where(l => l.ModuleId == currentLesson.ModuleId && l.LessonIndex > currentLesson.LessonIndex)
            .OrderBy(l => l.LessonIndex)
            .FirstOrDefaultAsync();


        if (nextLesson != null)
           
            await OpenLessonAsync(userId, nextLesson.Id);

        else
            await UnlockNextModuleAsync(userId, currentLesson.ModuleId);
    }


    public async Task UnlockNextModuleAsync(string userId, int currentModuleId)
    {
        Console.WriteLine($"---> Початок UnlockNextModule для модуля {currentModuleId}");

        // 1. Шукаємо прогрес поточного модуля
        var currentModuleProgress = await context.UserProgresses
            .Include(p => p.Module)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == currentModuleId && m.LessonId == null);

        if (currentModuleProgress == null)
        {
            Console.WriteLine(" Помилка: Не знайдено запис прогресу для поточного модуля (LessonId = 0)");
            return;
        }

        // 2. Позначаємо поточний як завершений
        currentModuleProgress.Status = ProgressStatus.Completed;
        currentModuleProgress.IsCompleted = true;
        Console.WriteLine($" Модуль {currentModuleId} позначено як Completed");

        // 3. Шукаємо наступний модуль
        var nextModule = await context.Modules
            .Where(m => m.CourseId == currentModuleProgress.Module!.CourseId &&
                        m.OrderIndex > currentModuleProgress.Module.OrderIndex)
            .OrderBy(m => m.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextModule == null)
        {
            Console.WriteLine(" Наступних модулів більше немає. Курс завершено!");
            await context.SaveChangesAsync();
            return;
        }

        Console.WriteLine($"---> Знайдено наступний модуль: {nextModule.Id} (Index: {nextModule.OrderIndex})");

        // 4. Перевірка оплати
        var registration = await context.CourseRegistrations
            .FirstOrDefaultAsync(cr => cr.UserId == userId && cr.CourseId == nextModule.CourseId);

        var isPaid = registration?.IsPaid ?? false;
        Console.WriteLine($" Статус оплати: {isPaid}");

        if (nextModule.OrderIndex > 1 && !isPaid)
        {
            Console.WriteLine(" Доступ заблоковано: Потрібна оплата для наступного модуля.");
            await context.SaveChangesAsync();
            return;
        }

        // 5. Відкриваємо наступний модуль
        var nextModuleProgress = await context.UserProgresses
            .FirstOrDefaultAsync(m => m.UserId == userId && m.ModuleId == nextModule.Id && m.LessonId == null);

        if (nextModuleProgress != null)
        {
            nextModuleProgress.Status = ProgressStatus.InProgress;
            Console.WriteLine($" Наступний модуль {nextModule.Id} переведено в InProgress");
        }

        // 6. Відкриваємо першу лекцію
        var firstLesson = await context.UserProgresses
            .Where(p => p.UserId == userId && p.ModuleId == nextModule.Id && p.LessonId != null)
            .OrderBy(p => p.LessonId)
            .FirstOrDefaultAsync();

        if (firstLesson != null)
        {
            firstLesson.Status = ProgressStatus.Open;
            Console.WriteLine($" Перша лекція {firstLesson.LessonId} відкрита");
        }

        await context.SaveChangesAsync();
        Console.WriteLine(" Зміни збережено в БД успішно!");
    }


    public async Task<bool> IsFirstModuleCompletedAsync(string userId, int courseId)
    {
        // Отримуємо перший модуль
        var firstModule = await context.Modules
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .FirstOrDefaultAsync();

        if (firstModule == null) return false;

        // Рахуємо всі лекції цього модуля
        var totalLessons = await context.Lessons.CountAsync(l => l.ModuleId == firstModule.Id);

        // Рахуємо завершені лекції цього користувача в цьому модулі
        var completedLessons = await context.UserProgresses
            .CountAsync(up => up.UserId == userId &&
                              up.ModuleId == firstModule.Id &&
                              up.Status == ProgressStatus.Completed);

        return totalLessons > 0 && completedLessons >= totalLessons;
    }

    public async Task SyncProgressAfterPayment(string userId, int courseId)
    {
        Console.WriteLine($"---> Сервіс: Початок синхронізації для User: {userId}");

        // 1. Примусово відкриваємо другий модуль
        var secondModule = await context.Modules
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .Skip(1)
            .FirstOrDefaultAsync();

        if (secondModule != null)
        {
            var secondModuleProgress = await context.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == secondModule.Id && p.LessonId == null);

            if (secondModuleProgress == null)
            {
                context.UserProgresses.Add(new UserProgress
                {
                    UserId = userId,
                    CourseId = courseId,
                    ModuleId = secondModule.Id,
                    LessonId = null,
                    Status = ProgressStatus.InProgress,
                    IsCompleted = false
                });
                Console.WriteLine("---> Сервіс: Створено прогрес для Другого Модуля");
            }

            // 2. Примусово відкриваємо першу лекцію другого модуля
            var firstLessonOfSecondModule = await context.Lessons
                .Where(l => l.ModuleId == secondModule.Id)
                .OrderBy(l => l.LessonIndex)
                .FirstOrDefaultAsync();

            if (firstLessonOfSecondModule != null)
            {
                var lessonProgress = await context.UserProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == firstLessonOfSecondModule.Id);

                if (lessonProgress == null)
                {
                    context.UserProgresses.Add(new UserProgress
                    {
                        UserId = userId,
                        CourseId = courseId,
                        ModuleId = secondModule.Id,
                        LessonId = firstLessonOfSecondModule.Id,
                        Status = ProgressStatus.InProgress,
                        IsCompleted = false
                    });
                    Console.WriteLine("---> Сервіс: Створено прогрес для Першої Лекції 2-го модуля");
                }
                else
                {
                    lessonProgress.Status = ProgressStatus.InProgress;
                }
            }
        }

     
        var modules = await context.Modules
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        foreach (var module in modules)
        {
            var lessonsProgress = await context.UserProgresses
                .Where(p => p.UserId == userId && p.ModuleId == module.Id && p.LessonId != null)
                .ToListAsync();

            var isAllLessonsCompleted =
                lessonsProgress.Any() && lessonsProgress.All(p => p.Status == ProgressStatus.Completed);

            if (isAllLessonsCompleted)
            {
                var moduleProgress = await context.UserProgresses
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == module.Id && p.LessonId == null);

                if (moduleProgress != null && moduleProgress.Status != ProgressStatus.Completed)
                {
                    moduleProgress.Status = ProgressStatus.Completed;
                    moduleProgress.IsCompleted = true;
                }

                await UnlockNextModuleAsync(userId, module.Id);
            }
            else
            {
                break;
            }
        }

        Console.WriteLine("---> Сервіс: Виклик SaveChangesAsync()");
        await context.SaveChangesAsync();
    }
}