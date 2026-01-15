namespace Market.DTOs
{
    public class OrderResultDto
    {
        public int OrderId { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public string Message { get; set; } = "Zakup udany";
    }
}