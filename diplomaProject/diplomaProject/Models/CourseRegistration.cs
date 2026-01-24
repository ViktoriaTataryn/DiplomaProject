namespace diplomaProject.Models
{
    public class CourseRegistration
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        //public User User { get; set; }
        public int CourseId { get; set; }
        // public Course Course { get; set; }
        public DateTime RegisterAt { get; set; } = DateTime.UtcNow;
    }
}
