namespace diplomaProject.DTOs
{
    public class UpdateModuleDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public string? ImageForUser { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public List<string> LessonNames { get; set; }
    }

    //public class LessonOrderDTO
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; }
    //    public int OrderIndex { get; set; } // Новий порядок, який встановить адмін
    //}
}
