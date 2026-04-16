namespace diplomaProject.DTOs
{
    public class HomeworkStatsDTO
    {
        public int TotalWork { get; set; }
        public int AvailableWork { get; set; }
        public int Completed { get; set; }
        public int Remaining  => TotalWork - Completed;
    }
}
