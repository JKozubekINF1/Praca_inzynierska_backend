using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models
{
    public class AnnouncementFeature
    {
        public int Id { get; set; }
        public string FeatureName { get; set; } = string.Empty;

        [ForeignKey("Announcement")]
        public int AnnouncementId { get; set; }
        public Announcement? Announcement { get; set; }
    }
}