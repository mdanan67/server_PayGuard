using System.ComponentModel.DataAnnotations;

namespace server.Dto
{
    public class EditProfileDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(30, ErrorMessage = "First name cannot exceed 30 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(30, ErrorMessage = "Last name cannot exceed 30 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30, ErrorMessage = "Phone cannot exceed 30 characters")]
        public string Phone { get; set; } = string.Empty;

        public string ProfileImage { get; set; } = string.Empty;
    }
}
