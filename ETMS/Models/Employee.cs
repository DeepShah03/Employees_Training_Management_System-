namespace ETMS.Models
{
    public class EmployeeViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public List<EmployeeModule> AssignedModules { get; set; } = new List<EmployeeModule>(); // Ensure proper initialization
    }


}
