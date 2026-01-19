using System.ComponentModel.DataAnnotations;

namespace Market.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } 

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } 

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }
}