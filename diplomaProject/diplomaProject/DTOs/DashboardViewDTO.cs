namespace diplomaProject.DTOs
{
    public class DashboardViewDTO
    {
        public DashboardProgressDTO Progress { get; set; }
        public UserProfileDTO UserProfile { get; set; }

        public List<int> Grades { get; set; }
    }
}
