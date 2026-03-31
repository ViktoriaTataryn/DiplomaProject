using Microsoft.Identity.Client;

namespace diplomaProject.DTOs
{
    public class ModuleProgressDTO
    {
        public int ModuleId { get; set; }
        public int ModuleNumber { get; set; }
        public string Name { get; set; }
       // public IFormFile? ImageUrl { get; set; }
        public string? ImageForUser { get; set; }
        public int CompletedLesson {  get; set; }
        public int TotalLesson { get; set; }
        public string Status { get; set; }
        public double Percent {  get; set; }
       // public double Percent => TotalLesson > 0 ? (double)CompletedLesson / TotalLesson * 100 : 0;
    }
}
