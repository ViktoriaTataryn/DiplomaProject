using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class HomeworkSubmission
    {
        public int Id { get; set; }

        // Связь с конкретным заданием (на что отвечает студент)
        public int HomeworkId { get; set; }
        public Homework Homework { get; set; }

        // Связь со студентом (кто именно сдал работу)
        public string StudentId { get; set; }
        public ApplicationUser Student { get; set; }

        [Required]
        [Display(Name = "File Path")]
        public string FilePath { get; set; } // Путь к загруженному файлу с выполненной работой

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; } = DateTime.Now; // Дата и время сдачи

        [Display(Name = "Grade")]
        public int? Grade { get; set; } // Оценка (со знаком вопроса, так как изначально работы не проверены)
    }
}