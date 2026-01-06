using System.ComponentModel.DataAnnotations;

namespace Market.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactPreference { get; set; } = "Telefon";
        public string Location { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string? PhotoUrl { get; set; }
        public VehicleDetails? VehicleDetails { get; set; }
        public PartDetails? PartDetails { get; set; }
        public ICollection<AnnouncementFeature> Features { get; set; } = new List<AnnouncementFeature>();
        public ICollection<AnnouncementPhoto> Photos { get; set; } = new List<AnnouncementPhoto>();
    }
}