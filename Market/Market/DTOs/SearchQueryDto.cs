namespace Market.DTOs // <--- ZWRÓĆ UWAGĘ: Musi być krótko, samo Market.DTOs
{
    public class SearchQueryDto
    {
        public string? Query { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public string? Category { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
    }

    public class SearchResultDto
    {
        public int TotalHits { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<object> Items { get; set; }
    }
}