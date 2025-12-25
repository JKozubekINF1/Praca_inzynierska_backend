using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models
{
    public class VehicleDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Announcement")]
        public int AnnouncementId { get; set; }
        public Announcement? Announcement { get; set; }
        public string Brand { get; set; } = string.Empty;     
        public string Model { get; set; } = string.Empty;    
        public string Generation { get; set; } = string.Empty; 
        public int Year { get; set; }
        public int Mileage { get; set; }
        public int EnginePower { get; set; }                    
        public int EngineCapacity { get; set; }                 
        public string FuelType { get; set; } = string.Empty;    
        public string Gearbox { get; set; } = string.Empty;    
        public string BodyType { get; set; } = string.Empty;   
        public string DriveType { get; set; } = string.Empty;   
        public string Color { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;        
        public string State { get; set; } = "Używany";       
    }
}