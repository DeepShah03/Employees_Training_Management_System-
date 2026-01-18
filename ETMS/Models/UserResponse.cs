using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class UserResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string SelectedOption { get; set; }

        public DateTime SubmittedAt { get; set; }

        [Required]
        public string TestType { get; set; } // Ensure TestType is included

        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
    }
}
