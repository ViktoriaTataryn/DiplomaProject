using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class Homework
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Task Description")]
        public string Description { get; set; } // Описание того, что нужно сделать

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; } // Крайний срок сдачи (дедлайн)

        // Связь с уроком: к какому уроку относится эта домашка
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; }

        // Список ответов, которые пришлют студенты
        public ICollection<HomeworkSubmission> Submissions { get; set; } = new List<HomeworkSubmission>();
    }
}