namespace Market.Models
{
    public class AnnouncementIndexModel
    {
        public string ObjectID { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public int? Year { get; set; }
        public int? Mileage { get; set; }
    }
}