namespace diplomaProject.Models
{
    public class Resource
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public Lesson Lesson { get; set; }
        public int LessonId { get; set; }
    }
}
