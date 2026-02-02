using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace Market.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public required string Email { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Surname { get; set; }

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = "User";

        public bool HasCompletedProfilePrompt { get; set; } = false;

        public bool IsBanned { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;
        public ICollection<Announcement>? Announcements { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        public bool IsEmailConfirmed { get; set; } = false;
        public string? ActivationToken { get; set; }
    }
}