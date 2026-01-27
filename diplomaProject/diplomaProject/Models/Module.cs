namespace diplomaProject.Models
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public Course Course { get; set; }
        public int CourseId { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
    }
}
