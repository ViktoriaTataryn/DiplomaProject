namespace diplomaProject.DTOs
{
    public class DashboardViewDTO
    {
        public string CourseTitle { get; set; }
        public int CourseId { get; set; }
        public DashboardProgressDTO Progress { get; set; }
        public UserProfileDTO UserProfile { get; set; }

        public List<GradeDTO> Grades { get; set; } = new List<GradeDTO>();
        public HomeworkStatsDTO HomeworkStats { get; set; }

        public bool HasStarted { get; set; }        // Чи розпочав користувач хоча б одну лекцію
        public bool FirstModuleCompleted { get; set; } // Чи пройдено 1-й модуль
        public bool IsPaid { get; set; }
        //public string NextLessonTitle { get; set; }
    }
}
