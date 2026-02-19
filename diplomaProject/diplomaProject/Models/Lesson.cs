using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lesson Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Video URL")]
        public string? VideoUrl { get; set; } // Ссылка на видео, если будет нужно

        // Связь с модулем к которому относится урок
        public Module Module { get; set; }
        public int ModuleId { get; set; }

        // Список всех файлов (картинок, методичек), привязанных к уроку
        [Display(Name = "Resources")]
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    }
}