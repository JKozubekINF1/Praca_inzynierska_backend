namespace Market.DTOs
{
    public class AnnouncementDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string Location { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactPreference { get; set; } = "Telefon";
        public UserSummaryDto User { get; set; } = null!;
        public VehicleDetailsDto? VehicleDetails { get; set; }
        public PartDetailsDto? PartDetails { get; set; }
        public List<AnnouncementPhotoDto> Photos { get; set; } = new();
        public List<AnnouncementFeatureDto> Features { get; set; } = new();
    }

    public class AnnouncementPhotoDto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }

    public class AnnouncementFeatureDto
    {
        public int Id { get; set; }
        public string FeatureName { get; set; } = string.Empty;
    }

    public class UserSummaryDto
    {
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}