using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            _indexName = configuration["Algolia:IndexName"] ?? "announcements";
            _logger = logger;
        }

        public async Task ConfigureIndexSettingsAsync()
        {
            try
            {
                var index = _client.InitIndex(_indexName);
                var settings = new IndexSettings
                {
                    NumericAttributesForFiltering = new List<string>
                    {
                        "expiresAt",
                        "price",
                        "year",
                        "mileage",
                        "enginePower",
                        "engineCapacity"
                    },

                    AttributesForFaceting = new List<string>
                    {
                        "filterOnly(isActive)",
                        "searchable(category)",
                        "searchable(brand)",
                        "searchable(model)",
                        "searchable(fuelType)"
                    }
                };

                await index.SetSettingsAsync(settings);
                _logger.LogInformation("Wysłano ustawienia indeksu do Algolii (camelCase).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas konfigurowania indeksu Algolii.");
                throw; 
            }
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
                    Category = a.Category,
                    PhotoUrl = a.PhotoUrl,
                    IsActive = a.IsActive,
                    ExpiresAt = ((DateTimeOffset)a.ExpiresAt).ToUnixTimeSeconds()
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
                _logger.LogError(ex, "Błąd podczas indeksowania ogłoszenia {Id}.", a.Id);
            }
        }

        public async Task RemoveAsync(string objectId)
        {
            try
            {
                var index = _client.InitIndex(_indexName);
                await index.DeleteObjectAsync(objectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd usuwania obiektu {ObjectId}.", objectId);
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

            if (!string.IsNullOrEmpty(dto.Category)) filters.Add($"category:'{dto.Category}'");
            if (!string.IsNullOrEmpty(dto.Brand)) filters.Add($"brand:'{dto.Brand}'");
            if (!string.IsNullOrEmpty(dto.Model)) filters.Add($"model:'{dto.Model}'");

            filters.Add("isActive:true");
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            filters.Add($"expiresAt > {nowTimestamp}");

            var searchQ = new Algolia.Search.Models.Search.Query(dto.Query ?? "")
            {
                Filters = string.Join(" AND ", filters),
                Page = dto.Page,
                HitsPerPage = dto.PageSize
            };

            var result = await index.SearchAsync<AnnouncementIndexModel>(searchQ);

            var items = result.Hits.Select(h => new SearchResultItem
            {
                Id = h.Id,
                ObjectID = h.ObjectID,
                Title = h.Title,
                Price = h.Price,
                Category = h.Category,
                PhotoUrl = h.PhotoUrl,
                Brand = h.Brand,
                Model = h.Model,
                Year = h.Year,
                Mileage = h.Mileage,
                Location = "Polska"
            }).ToList();

            return new SearchResultDto
            {
                TotalHits = result.NbHits,
                TotalPages = result.NbPages,
                CurrentPage = result.Page,
                Items = items
            };
        }

        public async Task IndexManyAnnouncementsAsync(IEnumerable<Announcement> announcements)
        {
            var index = _client.InitIndex(_indexName);

            await ConfigureIndexSettingsAsync();

            var batch = new List<AnnouncementIndexModel>();

            foreach (var a in announcements)
            {
                var model = new AnnouncementIndexModel
                {
                    ObjectID = a.Id.ToString(),
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    Category = a.Category,
                    PhotoUrl = a.PhotoUrl,
                    IsActive = a.IsActive,
                    ExpiresAt = ((DateTimeOffset)a.ExpiresAt).ToUnixTimeSeconds()
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