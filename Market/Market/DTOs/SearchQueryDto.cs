namespace Market.DTOs
{
    public class SearchQueryDto
    {
        public string? Query { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Location { get; set; }
        public string? State { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Generation { get; set; } 
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }
        public int? MinMileage { get; set; }
        public int? MaxMileage { get; set; }
        public int? MinPower { get; set; }
        public int? MaxPower { get; set; }
        public string? FuelType { get; set; }
        public string? Gearbox { get; set; }
        public string? BodyType { get; set; }
        public string? DriveType { get; set; }
        public string? PartNumber { get; set; }   
        public string? Compatibility { get; set; } 
        public string? SortBy { get; set; }
    }

    public class SearchResultItem
    {
        public int Id { get; set; }
        public string ObjectID { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Location { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Generation { get; set; } 
        public int? Year { get; set; }
        public int? Mileage { get; set; }
        public string? FuelType { get; set; }  
        public string? Gearbox { get; set; }    
        public string? BodyType { get; set; }  
        public string? PartNumber { get; set; }    
        public string? Compatibility { get; set; }
        public string? State { get; set; }      
    }

    public class SearchResultDto
    {
        public int TotalHits { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<SearchResultItem> Items { get; set; } = new();
    }
}