namespace diplomaProject.Models
{
    public class LiveMeeting
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string MeetingLink { get; set; }
        public string? RecordUrl { get; set; }

    }
}
