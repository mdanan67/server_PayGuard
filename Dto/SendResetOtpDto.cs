using System.ComponentModel.DataAnnotations;

namespace server.Dto
{
    public class SendResetOtpDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string Email { get; set; } = string.Empty;
    }
}
