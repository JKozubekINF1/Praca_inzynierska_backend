using System.ComponentModel.DataAnnotations;

namespace Market.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public int AnnouncementId { get; set; }

        [Required]
        public string DeliveryMethod { get; set; } = "Personal"; 
        public string? DeliveryPointName { get; set; }
        public string? DeliveryAddress { get; set; }
    }
}