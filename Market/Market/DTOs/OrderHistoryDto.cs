namespace Market.DTOs
{
    public class OrderHistoryDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } 
        public string DeliveryMethod { get; set; }
        public string? DeliveryPointName { get; set; }
        public string? DeliveryAddress { get; set; }

        public string AnnouncementTitle { get; set; }
        public string? AnnouncementPhotoUrl { get; set; }
    }
}