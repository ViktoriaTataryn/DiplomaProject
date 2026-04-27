using diplomaProject.Models;

namespace diplomaProject.DTOs
{
    public class HomeworkDashboardDTO
    {
        public Homework CurrentHomework { get; set; }
       

        // 2. Статистика (Ваш прогрес)
        public HomeworkStatsDTO Stats { get; set; }
        public double AverageScore { get; set; }

        // 3. Список виконаних (Виконані завдання)
        public List<HomeworkSubmission> ExecutedHomeworks { get; set; }
        public List<Module> AllModules { get; set; }
    }
}
