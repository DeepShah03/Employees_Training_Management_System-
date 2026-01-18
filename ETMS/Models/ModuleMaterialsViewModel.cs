using System.Collections.Generic;

namespace ETMS.Models.ViewModels
{
    public class ModuleMaterialsViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public List<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();
    }
}
