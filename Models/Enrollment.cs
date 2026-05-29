using System.ComponentModel.DataAnnotations;

namespace AdaptiveLearningSystem.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int ModuleId { get; set; }

        [Display(Name = "Enrolled Date")]
        public DateTime EnrolledDate { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Active";

        // Navigation
        public ApplicationUser? User { get; set; }
        public LearningModule? Module { get; set; }
    }
}
