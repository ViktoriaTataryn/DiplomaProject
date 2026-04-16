using diplomaProject.Models;
using diplomaProject.DTOs;
namespace diplomaProject.Interfaces
{


    public interface IProgressService
    {
        Task<ProgressStatus> GetLessonStatusAsync(string userId, int lessonId);

        // Змінити Open -> InProgress (коли студент відкрив лекцію)
        Task LessonInProgressAsync(string userId, int lessonId);

        Task OpenLessonAsync(string userId, int lessonId);


        // Метод для розблокування наступної лекції (Close -> Open)
        Task UnlockNextLessonAsync(string userId, int currentLessonId);



        // Окремий метод для перевірки, чи закритий весь модуль
        Task UnlockNextModuleAsync(string userId, int currentModuleId);

        Task StartCourse(string userId, int courseId);

        Task<Lesson> GetActiveLessonAsync(string userId, int courseId);

        Task <HomeworkStatus>GetHomeworkStatusAsync(string userId, int homeworkId);
        Task<bool> IsFirstModuleCompletedAsync(string userId, int courseId);



    }
}
