using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    // класс для отслеживания успехов студента в обучении
    public class UserProgress
    {
        //ункальный ID записи о прогрессе
        [Key]
        public int Id { get; set; }

        //ссылка на ID пользователя (связь с таблицей User)
        [Required]
        [Display(Name = "User ID")]
        public string UserId { get; set; }

        //ссылка на ID курса (связь с таблицей Course)
        [Required]
        [Display(Name = "Course ID")]
        public int CourseId { get; set; }

        // наа сколько процйентов пройден курс (от 0 до 100)
        [Display(Name = "Completion Percentage")]
        public int CompletionPercentage { get; set; } = 0;

        // флаг: закончил ли студент курс полностью?
        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; } = false;

        // дата и время последнего захода в материалы курса
        [Display(Name = "Last Activity Date")]
        public DateTime LastActivity { get; set; } = DateTime.Now;
    }
}