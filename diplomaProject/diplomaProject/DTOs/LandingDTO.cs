namespace diplomaProject.DTOs;

public class LandingDto
{
    public required List<ModuleProgressDto> Modules { get; set; }
    public required List<ReviewDto> Reviews { get; set; }
}