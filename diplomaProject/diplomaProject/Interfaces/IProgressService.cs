using diplomaProject.Models;
namespace diplomaProject.Interfaces
{
    public interface IProgressService
    {
        Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId);

        // Змінити Open -> InProgress (коли студент відкрив лекцію)
        Task StartLessonAsync(string userId, int lessonId);

        // Змінити InProgress -> Completed 
        Task CompleteLessonAsync(string userId, int lessonId);

        // Метод для розблокування наступної лекції (Close -> Open)
        Task UnlockNextLessonAsync(string userId, int currentLessonId);

        // Метод, який викликає адмін/система після оцінки ДЗ
        Task CompleteLessonAndUnlockNextAsync(string userId, int currentLessonId);

        // Окремий метод для перевірки, чи закритий весь модуль
        Task UnlockNextModuleAsync(string userId, int currentModuleId);

        Task StartCourse(string userId, int courseId);
    }
}

