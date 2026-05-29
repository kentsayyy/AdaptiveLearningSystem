using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalModules { get; set; }
        public int TotalCompleted { get; set; }
        public double AverageScore { get; set; }
        public List<StudentProgress> StrugglingStudents { get; set; } = new();
        public List<StudentProgress> RecentProgress { get; set; } = new();
        public List<ApplicationUser> InactiveStudents { get; set; } = new();
    }
}
