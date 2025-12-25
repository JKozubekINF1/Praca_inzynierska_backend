using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Market.Models
{
    public class PartDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Announcement")]
        public int AnnouncementId { get; set; }
        public Announcement? Announcement { get; set; }

        public string PartName { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Compatibility { get; set; } = string.Empty;
        public string State { get; set; } = "Używany";
    }
}