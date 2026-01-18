using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ETMS.Models
{
    public class LearningMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }  // Stores the file location

        [Required]
        public string FileType { get; set; } // "PDF" or "Video"

        public DateTime UploadedOn { get; set; } = DateTime.Now;

      

        [Required]
        [Column("ModuleId")] // Ensures correct column name
        [ForeignKey(nameof(Module))]  // Ensures EF uses 'ModuleId'
        public int ModuleId { get; set; }

        public virtual TrainingModule Module { get; set; }

    }
}
