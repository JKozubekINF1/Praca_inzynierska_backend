using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models
{
    public enum OrderStatus
    {
        New,
        Paid,
        Shipped,
        Completed
    }

    public class Order
    {
        public int Id { get; set; }

        public int BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public virtual User Buyer { get; set; }

        public int AnnouncementId { get; set; }
        [ForeignKey("AnnouncementId")]
        public virtual Announcement Announcement { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Paid;
        public string DeliveryMethod { get; set; }
        public string? DeliveryPointName { get; set; }
        public string? DeliveryAddress { get; set; }
    }
}