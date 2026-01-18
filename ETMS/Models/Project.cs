using System;
using System.ComponentModel.DataAnnotations;

namespace ETMS.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ProjectName { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime Deadline { get; set; }
        public string TrainerId { get; set; } // ✅ Add Trainer ID
        public ApplicationUser Trainer { get; set; } // ✅ Navigation property

        public ICollection<EmployeeProject> AssignedEmployees { get; set; }

        public ICollection<ProjectSubmission> ProjectSubmissions { get; set; }

    }
}
