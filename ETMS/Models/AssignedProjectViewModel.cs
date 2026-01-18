using System;

namespace ETMS.Models
{
    public class AssignedProjectViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public bool HasSubmitted { get; set; } // Check if submission exists
    }
}
