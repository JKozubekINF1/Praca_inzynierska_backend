using Market.Models;

namespace Market.DTOs
{
    public class CreateAnnouncementDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; } 
        public string PhoneNumber { get; set; }
        public string ContactPreference { get; set; }
        public string Location { get; set; }
        public List<string> Features { get; set; } = new();

        public VehicleDetailsDto? VehicleDetails { get; set; }
        public PartDetailsDto? PartDetails { get; set; }

        public List<IFormFile>? Photos { get; set; }
    }

    public class VehicleDetailsDto
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Generation { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
        public int EnginePower { get; set; }
        public int EngineCapacity { get; set; }
        public string FuelType { get; set; }
        public string Gearbox { get; set; }
        public string BodyType { get; set; }
        public string DriveType { get; set; }
        public string Color { get; set; }
        public string VIN { get; set; }
        public string State { get; set; }
    }

    public class PartDetailsDto
    {
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public string Compatibility { get; set; }
        public string State { get; set; }
    }
}