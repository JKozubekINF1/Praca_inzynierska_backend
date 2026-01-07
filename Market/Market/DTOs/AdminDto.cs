using Market.Models;

namespace Market.DTOs.Admin
{
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsBanned { get; set; }
        public int AnnouncementCount { get; set; }
    }

    public class AdminAnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string Author { get; set; }
        public string PhotoUrl { get; set; }
    }

    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalAnnouncements { get; set; }
        public int NewAnnouncementsToday { get; set; }
        public List<SystemLog> RecentLogs { get; set; } 
    }
}