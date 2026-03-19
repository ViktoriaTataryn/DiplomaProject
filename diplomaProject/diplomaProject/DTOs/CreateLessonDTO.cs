using diplomaProject.Models;
using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs
{
    public class CreateLessonDTO
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }
        public string? Description { get; set; }
        //public string HomeworkDescription { get; set; }
        public int ModuleId { get; set; }

        // Файли (зображення до лекції)
        public List<IFormFile> ResourceFiles { get; set; } = new List<IFormFile>();
    }
}
