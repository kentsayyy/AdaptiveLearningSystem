using System.ComponentModel.DataAnnotations;

namespace AdaptiveLearningSystem.Models
{
    public class LearningModule
    {
        [Key]
        public int ModuleId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Module Title")]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Difficulty Level")]
        public string Level { get; set; } = "Basic";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Date Created")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Quiz>? Quizzes { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<StudentProgress>? Progresses { get; set; }

        // Assigned teacher
        public string? TeacherId { get; set; }
        public ApplicationUser? Teacher { get; set; }
    }
}
