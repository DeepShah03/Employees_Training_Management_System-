namespace ETMS.Models
{
    public class NotificationViewModel
    {
        public string RecipientEmail { get; set; } // Individual recipient email (Optional if sending bulk)
        public string Subject { get; set; }
        public string Message { get; set; }

        
        public string TestType { get; set; }   // Type of test (Pre-training, Mid-training, etc.)
        public DateTime Date { get; set; }     // Test date
        public string Time { get; set; }       // Test time
    }
}
