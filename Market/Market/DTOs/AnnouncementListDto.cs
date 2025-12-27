namespace Market.DTOs
{
    public class AnnouncementListDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }   
        public string Location { get; set; }   
        public string? PhotoUrl { get; set; }  
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }    
    }
}