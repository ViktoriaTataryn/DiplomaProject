using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs
{
    public class CreateModuleDTO
    {
        [Required]
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public int CourseId { get; set; } = 3;
    }
}
