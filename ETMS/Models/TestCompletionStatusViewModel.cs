namespace ETMS.Models
{
    public class TestCompletionStatusViewModel
    {
        public string TestType { get; set; } // ✅ Add this line
        public List<string> CompletedEmployees { get; set; }
        public List<string> RemainingEmployees { get; set; }

        public TestCompletionStatusViewModel()
        {
            CompletedEmployees = new List<string>();
            RemainingEmployees = new List<string>();
        }
    }
}
