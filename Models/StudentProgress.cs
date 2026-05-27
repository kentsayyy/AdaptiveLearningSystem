using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdaptiveLearningSystem.Models
{
    public class StudentProgress
    {
        [Key]
        public int ProgressId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ModuleId { get; set; }
        public int QuizId { get; set; }

        [Range(0, 100)]
        [Display(Name = "Quiz Score")]
        public double QuizScore { get; set; }

        [Display(Name = "Status")]
        public string CompletionStatus { get; set; } = "In Progress";

        [Display(Name = "Date Completed")]
        public DateTime DateCompleted { get; set; } = DateTime.Now;

        // Adaptive recommendation (computed property)
        [NotMapped]
        public string Recommendation =>
            QuizScore < 75 ? "Review Basic Lesson" : "Proceed to Advanced Lesson";

        // Navigation
        public ApplicationUser? User { get; set; }
        public LearningModule? Module { get; set; }
        public Quiz? Quiz { get; set; }
    }
}
