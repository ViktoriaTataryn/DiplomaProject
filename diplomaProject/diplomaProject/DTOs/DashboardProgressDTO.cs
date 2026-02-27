namespace diplomaProject.DTOs
{
    public class DashboardProgressDTO
    {
        public string CurrentLesson { get; set; }
        public int CompletedModule {  get; set; }
        public int TotalModule { get; set; }
        public List<ModuleProgressDTO> ModuleProgress { get; set; }
    }
}
