using System.ComponentModel.DataAnnotations;

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

        public ICollection<Announcement>? Announcements { get; set; }
    }
}