namespace diplomaProject.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        //public string? VideoUrl { get; set; }
        public Module Module { get; set; }
        public int ModuleId { get; set; }
        public ICollection<Resource>? Resources { get; set; }
        public int? LiveMeetingId { get; set; }
        public LiveMeeting? LiveMeeting { get; set; }

    }
}
