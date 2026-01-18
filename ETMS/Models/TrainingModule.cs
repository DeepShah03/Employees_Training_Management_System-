using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class TrainingModule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SubjectName { get; set; }

        [Required]
        public string TrainerId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        public virtual ICollection<EmployeeModule> AssignedEmployees { get; set; }
        public virtual ICollection<LearningMaterial> LearningMaterials { get; set; }

    }

    /*public class TrainingModule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SubjectName { get; set; }

        [Required]
        public string TrainerId { get; set; }  // Matches AspNetUsers.Id (VARCHAR 450)

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // ✅ Relationship with Trainer (User)
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        // ✅ Add this navigation property
        public virtual ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();
    }*/
}







/*using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class TrainingModule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SubjectName { get; set; }

        [Required]
        public string TrainerId { get; set; }  // Matches AspNetUsers.Id (VARCHAR 450)

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // Relationship with Trainer (User)
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }


    }
}
*/
