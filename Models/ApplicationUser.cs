using Microsoft.AspNetCore.Identity;

namespace AdaptiveLearningSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime DateRegistered { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<StudentProgress>? StudentProgresses { get; set; }
    }
}
