namespace Market.Models
{
    public class AnnouncementIndexModel
    {
        public string ObjectID { get; set; } 
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; } = "https://via.placeholder.com/150"; 

        
        public string? Brand { get; set; }
        public int? Year { get; set; }
        public int? Mileage { get; set; }
    }
}