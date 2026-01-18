using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class TestSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("ApplicationUser")]
        public string UserId { get; set; }  // Ensure it's string (VARCHAR(450))

        [Required]
        public string TestType { get; set; }
        public int Marks { get; set; } // ✅ Add marks field

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
