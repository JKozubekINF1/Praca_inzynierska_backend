using System.ComponentModel.DataAnnotations;

namespace Market.DTOs
{
    public class TopUpDto
    {
        [Range(0.01, 100000, ErrorMessage = "Kwota doładowania musi być większa od 0.")]
        public decimal Amount { get; set; }
    }
}