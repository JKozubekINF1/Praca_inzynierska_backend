using Algolia.Search.Clients;
using Market.DTOs;
using Market.Interfaces;
using Market.Models; 

namespace Market.Services
{
    public class AlgoliaSearchService : ISearchService
    {
        private readonly ISearchClient _client;
        private readonly string _indexName;
        private readonly ILogger<AlgoliaSearchService> _logger;

        public AlgoliaSearchService(ISearchClient client, IConfiguration configuration, ILogger<AlgoliaSearchService> logger)
        {
            _client = client;
            _indexName = configuration["Algolia:IndexName"];
            _logger = logger;
        }

        public async Task IndexAnnouncementAsync(Announcement a)
        {
            try
            {
                var index = _client.InitIndex(_indexName);

                var indexModel = new AnnouncementIndexModel
                {
                    ObjectID = a.Id.ToString(),
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    Category = a.Category
                };

                if (a.VehicleDetails != null)
                {
                    indexModel.Brand = a.VehicleDetails.Brand;
                    indexModel.Model = a.VehicleDetails.Model;
                    indexModel.Year = a.VehicleDetails.Year;
                    indexModel.Mileage = a.VehicleDetails.Mileage;
                }

                await index.SaveObjectAsync(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas indeksowania ogłoszenia {Id} w Algolii.", a.Id);
            }
        }


        public async Task<SearchResultDto> SearchAsync(SearchQueryDto dto)
        {
            var index = _client.InitIndex(_indexName);
            var filters = new List<string>();

            if (dto.MinPrice.HasValue) filters.Add($"price >= {dto.MinPrice}");
            if (dto.MaxPrice.HasValue) filters.Add($"price <= {dto.MaxPrice}");
            if (dto.MinYear.HasValue) filters.Add($"year >= {dto.MinYear}");
            if (dto.MaxYear.HasValue) filters.Add($"year <= {dto.MaxYear}");

            if (!string.IsNullOrEmpty(dto.Category)) filters.Add($"category:{dto.Category}");
            if (!string.IsNullOrEmpty(dto.Brand)) filters.Add($"brand:{dto.Brand}");
            if (!string.IsNullOrEmpty(dto.Model)) filters.Add($"model:{dto.Model}");

            var searchQ = new Algolia.Search.Models.Search.Query(dto.Query ?? "")
            {
                Filters = string.Join(" AND ", filters),
                Page = dto.Page,
                HitsPerPage = dto.PageSize
            };

            var result = await index.SearchAsync<AnnouncementIndexModel>(searchQ);

            return new SearchResultDto
            {
                TotalHits = result.NbHits,
                TotalPages = result.NbPages,
                CurrentPage = result.Page,
                Items = result.Hits
            };
        }


        public async Task IndexManyAnnouncementsAsync(IEnumerable<Announcement> announcements)
        {
            var index = _client.InitIndex(_indexName);
            var batch = new List<AnnouncementIndexModel>();

            foreach (var a in announcements)
            {
                var model = new AnnouncementIndexModel
                {
                    ObjectID = a.Id.ToString(),
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    Category = a.Category
                };

                if (a.VehicleDetails != null)
                {
                    model.Brand = a.VehicleDetails.Brand;
                    model.Model = a.VehicleDetails.Model;
                    model.Year = a.VehicleDetails.Year;
                    model.Mileage = a.VehicleDetails.Mileage;
                }

                batch.Add(model);
            }

            if (batch.Any())
            {
                await index.SaveObjectsAsync(batch);
            }
        }
    }
}