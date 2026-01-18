using System.ComponentModel.DataAnnotations;
namespace ETMS.Models

{
    public class TestModeUpdate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "TestType is required.")]
        public string TestType { get; set; }

        [Required(ErrorMessage = "Mode is required.")]
        public string Mode { get; set; }
    }

}
