using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class CreateLessonDto
{
    [Required] public required string Title { get; set; }

    [Required] public required string Content { get; set; }

    public string Description { get; set; } = "";

    //public string HomeworkDescription { get; set; }
    public int LessonIndex { get; set; }
    public int ModuleId { get; set; }

    // Файли (зображення до лекції)
    public List<IFormFile>? ResourceFiles { get; set; } = [];
}