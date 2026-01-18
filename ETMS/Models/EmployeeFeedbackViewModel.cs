using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ETMS.Models
{
    public class EmployeeFeedbackViewModel
    {
        [Required]
        public string EmployeeId { get; set; }

        public int TestMarks { get; set; } // Auto-filled from DB

        [Required]
        public int ProjectMarks { get; set; } // Trainer fills in project marks

        [Required]
        [StringLength(500)]
        public string Feedback { get; set; } // Trainer provides additional feedback

        // ✅ New: List of selected criteria (from checkboxes)
        public List<string> SelectedCriteria { get; set; } = new List<string>();

        public List<EmployeeReport> Reports { get; set; } = new List<EmployeeReport>(); // List of generated reports
    }
}














/*using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ETMS.Models
{
    public class EmployeeFeedbackViewModel
    {
        [Required]
        public string EmployeeId { get; set; }

        public int TestMarks { get; set; } // Auto-filled from DB

        [Required]
        public int ProjectMarks { get; set; } // Trainer fills in project marks

        [Required]
        [StringLength(500)]
        public string Feedback { get; set; } // Trainer provides feedback

        public List<EmployeeReport> Reports { get; set; } = new List<EmployeeReport>(); // List of generated reports
    }
}
*/