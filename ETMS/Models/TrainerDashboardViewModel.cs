using System.Collections.Generic;

namespace ETMS.Models.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public string TrainerName { get; set; }
        public int TotalTrainingModules { get; set; }
        public List<TrainingModule> TrainingModules { get; set; } = new List<TrainingModule>();
    }
}
