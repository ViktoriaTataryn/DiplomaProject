using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class CreateModuleDto
{
    [Required] public required string Title { get; set; }

    public int OrderIndex { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageFile { get; set; }
}