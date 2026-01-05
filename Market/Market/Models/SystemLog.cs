namespace Market.Models
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string Action { get; set; } 
        public string Message { get; set; } 
        public string? Username { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}