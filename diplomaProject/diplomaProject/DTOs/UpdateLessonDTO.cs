using diplomaProject.Models;

namespace diplomaProject.DTOs;

public class UpdateLessonDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ModuleId { get; set; }

    // Файли (зображення до лекції)
    public List<IFormFile> NewResourceFiles { get; set; } = new();

    //  для відображення існуючих файлів на сторінці (тільки для читання)
    public List<Resource> ExistingResources { get; set; } = new();

    // Список ID або посилань на файли, які адмін хоче ВИДАЛИТИ
    public List<int>? DeletedResourceIds { get; set; } = new();
}