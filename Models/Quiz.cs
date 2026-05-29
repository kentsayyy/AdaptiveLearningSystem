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

        [Required]
        [Display(Name = "Deadline")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);

        // Navigation
        public LearningModule? Module { get; set; }
        public ICollection<StudentProgress>? Progresses { get; set; }
    }
}
