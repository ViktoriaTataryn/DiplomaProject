using diplomaProject.Models;
using System.Collections.Generic;

namespace diplomaProject.DTOs
{
    public class HomeworkDashboardDTO
    {
        // 1. Current task info
        public Homework CurrentHomework { get; set; }

        // 2. Statistics (Your progress)
        public HomeworkStatsDTO Stats { get; set; }
        public double AverageScore { get; set; }

        // 3. Lists for the UI
        public List<HomeworkSubmission> ExecutedHomeworks { get; set; }
        public List<Module> AllModules { get; set; }
    }
}