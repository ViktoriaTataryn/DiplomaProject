namespace diplomaProject.DTOs;

public class HomeworkStatsDto
{
    public int TotalWork { get; set; }
    public int AvailableWork { get; set; }
    public int Completed { get; set; }
    public int Remaining => TotalWork - Completed;
}