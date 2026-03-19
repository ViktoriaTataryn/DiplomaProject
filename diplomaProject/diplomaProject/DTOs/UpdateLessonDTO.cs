using diplomaProject.Models;
using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs
{
    public class UpdateLessonDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string Content { get; set; }
        public string? Description { get; set; }
        public int ModuleId { get; set; }

        // Файли (зображення до лекції)
        public List<IFormFile> NewResourceFiles { get; set; } = new List<IFormFile>();

        //  для відображення існуючих файлів на сторінці (тільки для читання)
        public List<Resource> ExistingResources { get; set; } = new List<Resource>();

        // Список ID або посилань на файли, які адмін хоче ВИДАЛИТИ
        public List<int>? DeletedResourceIds { get; set; } = new List<int>();
    }
}
