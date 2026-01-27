namespace diplomaProject.Models
{
    public class CourseRegistration
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public DateTime RegisterAt { get; set; } = DateTime.UtcNow;
    }
}
