using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Market.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsNegotiable { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ContactPreference { get; set; } = string.Empty;

        [Required]
        public string TypeSpecificData { get; set; } = string.Empty;

        public bool IsActive => DateTime.UtcNow <= ExpiresAt;
    }

    public class AnnouncementDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public bool IsNegotiable { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ContactPreference { get; set; } = string.Empty;

        public VehicleData? VehicleData { get; set; }
        public PartData? PartData { get; set; }
    }

    public class VehicleData
    {
        [Required]
        [MaxLength(50)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [Range(1900, 2100)]
        public int Year { get; set; }

        [MaxLength(50)]
        public string? EquipmentVersion { get; set; }

        [Required]
        [MaxLength(50)]
        public string BodyType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Engine { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FuelType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Transmission { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Drive { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Mileage { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Condition { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string History { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Equipment { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Formalities { get; set; } = string.Empty;
    }

    public class PartData
    {
        [Required]
        [MaxLength(100)]
        public string PartName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PartNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string Condition { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Compatibility { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EngineVersion { get; set; }
    }
}