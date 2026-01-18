using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class EmployeeReport
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string EmployeeId { get; set; }  // ✅ Ensure it's a string

        [Required]
        public string TrainerId { get; set; }  // ✅ ADD TrainerId

        public int PreTrainingMarks { get; set; } = -1;
        public int MidTrainingMarks { get; set; } = -1;
        public int PostTrainingMarks { get; set; } = -1;
        public int ProjectMarks { get; set; }
        public string Feedback { get; set; }
        public string FeedbackCriteria { get; set; } = "";



        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Add navigation properties
        [ForeignKey("EmployeeId")]
        public virtual ApplicationUser Employee { get; set; }

        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }
    }
}



