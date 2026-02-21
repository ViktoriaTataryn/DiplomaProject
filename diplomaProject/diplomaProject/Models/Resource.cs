using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class Resource
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "File Name")]
        public string FileName { get; set; }

        [Required]
        [Display(Name = "File Path")]
        public string FilePath { get; set; }

        // Навигационные свойства для соответствующего урока
        public Lesson Lesson { get; set; }

        [Display(Name = "Lesson ID")]
        public int LessonId { get; set; }
    }
}