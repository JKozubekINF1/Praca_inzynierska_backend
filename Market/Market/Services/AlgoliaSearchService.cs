using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;
using Market.DTOs;
using Market.Helpers;
using Market.Interfaces;
using Market.Models;

namespace Market.Services
{
    public class AlgoliaSearchService : ISearchService
    {
        private readonly ISearchClient _client;
        private readonly string _baseIndexName;
        private readonly ILogger<AlgoliaSearchService> _logger;

        public AlgoliaSearchService(ISearchClient client, IConfiguration configuration, ILogger<AlgoliaSearchService> logger)
        {
            _client = client;
            _baseIndexName = configuration["Algolia:IndexName"] ?? "announcements";
            _logger = logger;
        }

        public async Task ConfigureIndexSettingsAsync()
        {
            try
            {
                var index = _client.InitIndex(_baseIndexName);

                var filterAttributes = new List<string>
                {
                    "expiresAt", "createdAtTimestamp", "price",
                    "year", "mileage", "enginePower", "engineCapacity"
                };

                var facetAttributes = new List<string>
                {
                    "filterOnly(isActive)",
                    "searchable(category)",
                    "searchable(location)",
                    "searchable(state)",
                    "searchable(brand)",
                    "searchable(model)",
                    "searchable(generation)",
                    "searchable(fuelType)",
                    "searchable(gearbox)",
                    "searchable(bodyType)",
                    "searchable(driveType)",
                    "searchable(color)",
                    "searchable(partName)",
                    "searchable(partNumber)",
                    "searchable(compatibility)"
                };

                var settings = new IndexSettings
                {
                    NumericAttributesForFiltering = filterAttributes,
                    AttributesForFaceting = facetAttributes,
                    Replicas = new List<string>
                    {
                        $"{_baseIndexName}_price_asc",
                        $"{_baseIndexName}_price_desc",
                        $"{_baseIndexName}_date_desc"
                    }
                };

                await index.SetSettingsAsync(settings);

                await ConfigureReplicaAsync($"{_baseIndexName}_price_asc", "price", "asc", filterAttributes, facetAttributes);
                await ConfigureReplicaAsync($"{_baseIndexName}_price_desc", "price", "desc", filterAttributes, facetAttributes);
                await ConfigureReplicaAsync($"{_baseIndexName}_date_desc", "createdAtTimestamp", "desc", filterAttributes, facetAttributes);

                _logger.LogInformation("Algolia index configured successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring Algolia index.");
                throw;
            }
        }

        private async Task ConfigureReplicaAsync(string replicaName, string attribute, string direction, List<string> filterAttributes, List<string> facetAttributes)
        {
            var index = _client.InitIndex(replicaName);
            var settings = new IndexSettings
            {
                Ranking = new List<string>
                {
                    $"{direction}({attribute})",
                    "typo", "geo", "words", "filters", "proximity", "attribute", "exact", "custom"
                },
                NumericAttributesForFiltering = filterAttributes,
                AttributesForFaceting = facetAttributes
            };
            await index.SetSettingsAsync(settings);
        }

        public async Task IndexAnnouncementAsync(Announcement a)
        {
            try
            {
                var index = _client.InitIndex(_baseIndexName);
                var indexModel = MapToAlgoliaModel(a);
                await index.SaveObjectAsync(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing announcement {Id}", a.Id);
            }
        }

        public async Task IndexManyAnnouncementsAsync(IEnumerable<Announcement> announcements)
        {
            var index = _client.InitIndex(_baseIndexName);
            var batch = announcements.Select(MapToAlgoliaModel).ToList();
            if (batch.Any()) await index.SaveObjectsAsync(batch);
        }

        public async Task RemoveAsync(string objectId)
        {
            try { await _client.InitIndex(_baseIndexName).DeleteObjectAsync(objectId); } catch { }
        }

        public async Task<SearchResultDto> SearchAsync(SearchQueryDto dto)
        {
            string targetIndex = _baseIndexName;
            if (dto.SortBy == "price_asc") targetIndex = $"{_baseIndexName}_price_asc";
            else if (dto.SortBy == "price_desc") targetIndex = $"{_baseIndexName}_price_desc";
            else if (dto.SortBy == "newest") targetIndex = $"{_baseIndexName}_date_desc";

            var index = _client.InitIndex(targetIndex);
            var filterBuilder = new AlgoliaFilterBuilder()
                .AddCondition("isActive:true")
                .AddExpirationCheck()
                .AddFacet("category", dto.Category)
                .AddFacet("location", dto.Location)
                .AddFacet("state", dto.State)
                .AddRange("price", dto.MinPrice, dto.MaxPrice);

            if (dto.Category == "Pojazd" || string.IsNullOrEmpty(dto.Category))
            {
                filterBuilder
                    .AddFacet("brand", dto.Brand)
                    .AddFacet("model", dto.Model)
                    .AddFacet("generation", dto.Generation)
                    .AddFacet("fuelType", dto.FuelType)
                    .AddFacet("gearbox", dto.Gearbox)
                    .AddFacet("bodyType", dto.BodyType)
                    .AddFacet("driveType", dto.DriveType)
                    .AddRange("year", dto.MinYear, dto.MaxYear)
                    .AddRange("mileage", dto.MinMileage, dto.MaxMileage)
                    .AddRange("enginePower", dto.MinPower, dto.MaxPower);
            }

            if (dto.Category == "Część")
            {
                filterBuilder.AddFacet("partNumber", dto.PartNumber);
            }

            var filterString = filterBuilder.Build();

            var query = new Algolia.Search.Models.Search.Query(dto.Query ?? "")
            {
                Filters = filterString,
                Page = dto.Page,
                HitsPerPage = dto.PageSize
            };

            var result = await index.SearchAsync<AnnouncementIndexModel>(query);

            var items = result.Hits.Select(h => new SearchResultItem
            {
                Id = h.Id,
                ObjectID = h.ObjectID,
                Title = h.Title,
                Price = h.Price,
                Category = h.Category,
                PhotoUrl = h.PhotoUrl,
                Location = h.Location,
                Brand = h.Brand,
                Model = h.Model,
                Generation = h.Generation,
                Year = h.Year,
                Mileage = h.Mileage,
                FuelType = h.FuelType,
                Gearbox = h.Gearbox,
                BodyType = h.BodyType,
                PartNumber = h.PartNumber,
                Compatibility = h.Compatibility,
                State = h.State
            }).ToList();

            return new SearchResultDto
            {
                TotalHits = result.NbHits,
                TotalPages = result.NbPages,
                CurrentPage = result.Page,
                Items = items
            };
        }

        private AnnouncementIndexModel MapToAlgoliaModel(Announcement a)
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
                ExpiresAt = ((DateTimeOffset)a.ExpiresAt).ToUnixTimeSeconds(),
                CreatedAtTimestamp = ((DateTimeOffset)a.CreatedAt).ToUnixTimeSeconds(),
                Location = a.Location
            };
            if (a.VehicleDetails != null)
            {
                var v = a.VehicleDetails;
                model.Brand = v.Brand;
                model.Model = v.Model;
                model.Generation = v.Generation;
                model.Year = v.Year;
                model.Mileage = v.Mileage;
                model.EnginePower = v.EnginePower;
                model.EngineCapacity = v.EngineCapacity;
                model.FuelType = v.FuelType;
                model.Gearbox = v.Gearbox;
                model.BodyType = v.BodyType;
                model.DriveType = v.DriveType;
                model.Color = v.Color;
                model.State = v.State;
            }

            if (a.PartDetails != null)
            {
                var p = a.PartDetails;
                model.PartName = p.PartName;
                model.PartNumber = p.PartNumber;
                model.Compatibility = p.Compatibility;
                model.State = p.State;
            }

            return model;
        }
    }
}