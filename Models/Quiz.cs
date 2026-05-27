using System.ComponentModel.DataAnnotations;

namespace AdaptiveLearningSystem.Models
{
    public class Quiz
    {
        public int QuizId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public int ModuleId { get; set; }

        [Display(Name = "Total Items")]
        public int TotalItems { get; set; } = 10;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        public LearningModule? Module { get; set; }
        public ICollection<StudentProgress>? Progresses { get; set; }
    }
}
