using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ETMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; } // Must be 'Admin', 'Trainer', or 'Employee'

        // Tracks which trainer is assigned to an employee
        public string? AssignedTrainerId { get; set; }
        public virtual ApplicationUser? Trainer { get; set; } // Self-reference

        // Employees assigned to this trainer
        public virtual List<ApplicationUser> AssignedEmployees { get; set; } = new List<ApplicationUser>();

        // Employee's assigned training modules
        public virtual List<EmployeeModule> AssignedModules { get; set; } = new List<EmployeeModule>();

        public ICollection<EmployeeProject> AssignedProjects { get; set; } = new List<EmployeeProject>();

        public virtual ICollection<EmployeeReport> EmployeeReports { get; set; }


    }
}





/*using Microsoft.AspNetCore.Identity;

namespace ETMS.Models
{
    *//*public class ApplicationUser : IdentityUser
    {
       
        public string Role { get; set; }  // Role must match one of 'Admin', 'Trainer', 'Employee'


        // Add AssignedTrainerId to track the trainer-employee relationship
        public string? AssignedTrainerId { get; set; }

        *//*public virtual List<EmployeeModule> AssignedModules { get; set; } = new List<EmployeeModule>();*//*
        public List<EmployeeModule> AssignedModules { get; set; } = new List<EmployeeModule>(); // Ensure List Type

    }*//*
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; } // Must be 'Admin', 'Trainer', or 'Employee'

        // Tracks which trainer is assigned to an employee
        public string? AssignedTrainerId { get; set; }
        public virtual ApplicationUser? Trainer { get; set; } // Self-reference

        // Employees assigned to this trainer
        public virtual List<ApplicationUser> AssignedEmployees { get; set; } = new List<ApplicationUser>();

        // Employee's assigned training modules
        public virtual List<EmployeeModule> AssignedModules { get; set; } = new List<EmployeeModule>();
    }

}
*/