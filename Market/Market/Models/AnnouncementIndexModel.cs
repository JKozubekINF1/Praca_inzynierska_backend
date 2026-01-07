namespace Market.Models
{
    public class AnnouncementIndexModel
    {
        public string ObjectID { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; }
        public long ExpiresAt { get; set; }
        public long CreatedAtTimestamp { get; set; }
        public string? Location { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Generation { get; set; }
        public int? Year { get; set; }
        public int? Mileage { get; set; }
        public int? EnginePower { get; set; }
        public double? EngineCapacity { get; set; }
        public string? FuelType { get; set; }
        public string? Gearbox { get; set; }
        public string? BodyType { get; set; }
        public string? DriveType { get; set; }
        public string? Color { get; set; }
        public string? PartName { get; set; }
        public string? PartNumber { get; set; }
        public string? Compatibility { get; set; }
        public string? State { get; set; }
    }
}