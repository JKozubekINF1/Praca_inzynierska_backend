using System.ComponentModel.DataAnnotations;

namespace Market.DTOs
{
    public class UserDto
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Surname { get; set; }

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        public bool HasCompletedProfilePrompt { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        [MinLength(8)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}