namespace Market.DTOs
{
    public class AnnouncementListDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }   // "Pojazd" lub "Część"
        public string Location { get; set; }   // Warto pokazać miasto na liście
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }     // Czy ogłoszenie jest nadal ważne

        // Opcjonalnie: Jeśli masz zdjęcia, tutaj przydałoby się zdjęcie główne
        // public string? ThumbnailUrl { get; set; } 
    }
}