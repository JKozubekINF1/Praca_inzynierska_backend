using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models
{
    public class AnnouncementPhoto
    {
        public int Id { get; set; }

        public string PhotoUrl { get; set; } = string.Empty; 
        public bool IsMain { get; set; }

        public int AnnouncementId { get; set; }
        [ForeignKey("AnnouncementId")]
        public Announcement? Announcement { get; set; }
    }
}