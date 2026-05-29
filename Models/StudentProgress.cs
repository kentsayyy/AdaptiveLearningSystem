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

        [Range(0, 10000)]
        [Display(Name = "Quiz Score")]
        public double QuizScore { get; set; }

        [Display(Name = "Status")]
        public string CompletionStatus { get; set; } = "In Progress";

        [Display(Name = "Date Completed")]
        public DateTime DateCompleted { get; set; } = DateTime.Now;

        [NotMapped]
        public string Recommendation =>
            Quiz != null && Quiz.TotalItems > 0
                ? ((QuizScore / Quiz.TotalItems) * 100 < 75
                    ? "Review Basic Lesson"
                    : "Proceed to Advanced Lesson")
                : (QuizScore < 75 ? "Review Basic Lesson" : "Proceed to Advanced Lesson");

        [NotMapped]
        public int? CorrectAnswers { get; set; }

        public ApplicationUser? User { get; set; }
        public LearningModule? Module { get; set; }
        public Quiz? Quiz { get; set; }
    }
}
