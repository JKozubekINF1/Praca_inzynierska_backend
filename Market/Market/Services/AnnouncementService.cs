using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization; 

namespace Market.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;
        private readonly ISearchService _searchService;
        private readonly IFileService _fileService;
        private readonly IAiModerationService _aiModerationService;
        private readonly ILogService _logService;

        public AnnouncementService(
            AppDbContext context,
            ISearchService searchService,
            IFileService fileService,
            IAiModerationService aiModerationService,
            ILogService logService)
        {
            _context = context;
            _searchService = searchService;
            _fileService = fileService;
            _aiModerationService = aiModerationService;
            _logService = logService;
        }

        private double? ParseCoordinate(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return null;
        }


        public async Task<int> CreateAsync(CreateAnnouncementDto dto, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            string username = user?.Username ?? "Unknown";

            var moderation = await _aiModerationService.CheckContentAsync(dto.Title, dto.Description, dto.Price);

            if (!moderation.IsSafe)
            {
                await _logService.LogAsync("AI_BLOCK", $"Zablokowano: {dto.Title}. Powód: {moderation.Reason}", username);
                throw new InvalidOperationException($"Ogłoszenie zablokowane. Powód: {moderation.Reason}");
            }

            var announcement = new Announcement
            {
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                Location = dto.Location,
                Latitude = ParseCoordinate(dto.Latitude),
                Longitude = ParseCoordinate(dto.Longitude),
                PhoneNumber = dto.PhoneNumber,
                ContactPreference = dto.ContactPreference,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                PhotoUrl = null
            };

            if (dto.Category == "Pojazd" && dto.VehicleDetails != null)
            {
                announcement.VehicleDetails = new VehicleDetails
                {
                    Brand = dto.VehicleDetails.Brand,
                    Model = dto.VehicleDetails.Model,
                    Generation = dto.VehicleDetails.Generation,
                    Year = dto.VehicleDetails.Year,
                    Mileage = dto.VehicleDetails.Mileage,
                    EnginePower = dto.VehicleDetails.EnginePower,
                    EngineCapacity = dto.VehicleDetails.EngineCapacity,
                    FuelType = dto.VehicleDetails.FuelType,
                    Gearbox = dto.VehicleDetails.Gearbox,
                    BodyType = dto.VehicleDetails.BodyType,
                    DriveType = dto.VehicleDetails.DriveType,
                    Color = dto.VehicleDetails.Color,
                    VIN = dto.VehicleDetails.VIN,
                    State = dto.VehicleDetails.State
                };
            }

            if (dto.Category == "Część" && dto.PartDetails != null)
            {
                announcement.PartDetails = new PartDetails
                {
                    PartName = dto.PartDetails.PartName,
                    PartNumber = dto.PartDetails.PartNumber,
                    Compatibility = dto.PartDetails.Compatibility,
                    State = dto.PartDetails.State
                };
            }

            if (dto.Features != null)
            {
                foreach (var featureName in dto.Features)
                {
                    announcement.Features.Add(new AnnouncementFeature { FeatureName = featureName });
                }
            }

            if (dto.Photos != null && dto.Photos.Count > 0)
            {
                bool isFirst = true;
                foreach (var file in dto.Photos)
                {
                    string path = await _fileService.SaveFileAsync(file);

                    var photo = new AnnouncementPhoto
                    {
                        PhotoUrl = path,
                        IsMain = isFirst
                    };
                    announcement.Photos.Add(photo);

                    if (isFirst)
                    {
                        announcement.PhotoUrl = path;
                        isFirst = false;
                    }
                }
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            await _searchService.IndexAnnouncementAsync(announcement);

            await _logService.LogAsync("NEW_ANNOUNCEMENT", $"Dodano ogłoszenie ID: {announcement.Id}: {dto.Title}", username);

            return announcement.Id;
        }

        public async Task<AnnouncementDto?> GetByIdAsync(int id)
        {
            var a = await _context.Announcements
                .Include(x => x.User)
                .Include(x => x.VehicleDetails)
                .Include(x => x.PartDetails)
                .Include(x => x.Features)
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return null;

            var dto = new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Price = a.Price,
                Category = a.Category,
                PhotoUrl = a.PhotoUrl,
                Location = a.Location,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                CreatedAt = a.CreatedAt,
                ExpiresAt = a.ExpiresAt,
                IsActive = a.IsActive,
                PhoneNumber = a.PhoneNumber,
                ContactPreference = a.ContactPreference,

                Photos = a.Photos.Select(p => new AnnouncementPhotoDto
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    IsMain = p.IsMain
                }).ToList(),

                Features = a.Features.Select(f => new AnnouncementFeatureDto
                {
                    Id = f.Id,
                    FeatureName = f.FeatureName
                }).ToList(),

                User = new UserSummaryDto
                {
                    Username = a.User.Username,
                    PhoneNumber = a.User.PhoneNumber,
                    Email = a.User.Email
                }
            };

            if (a.VehicleDetails != null)
            {
                dto.VehicleDetails = new VehicleDetailsDto
                {
                    Brand = a.VehicleDetails.Brand,
                    Model = a.VehicleDetails.Model,
                    Generation = a.VehicleDetails.Generation,
                    Year = a.VehicleDetails.Year,
                    Mileage = a.VehicleDetails.Mileage,
                    EnginePower = a.VehicleDetails.EnginePower,
                    EngineCapacity = a.VehicleDetails.EngineCapacity,
                    FuelType = a.VehicleDetails.FuelType,
                    Gearbox = a.VehicleDetails.Gearbox,
                    BodyType = a.VehicleDetails.BodyType,
                    DriveType = a.VehicleDetails.DriveType,
                    Color = a.VehicleDetails.Color,
                    VIN = a.VehicleDetails.VIN,
                    State = a.VehicleDetails.State
                };
            }

            if (a.PartDetails != null)
            {
                dto.PartDetails = new PartDetailsDto
                {
                    PartName = a.PartDetails.PartName,
                    PartNumber = a.PartDetails.PartNumber,
                    Compatibility = a.PartDetails.Compatibility,
                    State = a.PartDetails.State
                };
            }

            return dto;
        }

        public async Task<List<AnnouncementListDto>> GetUserAnnouncementsAsync(int userId)
        {
            return await _context.Announcements
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementListDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    Category = a.Category,
                    Location = a.Location,
                    PhotoUrl = a.PhotoUrl,
                    CreatedAt = a.CreatedAt,
                    ExpiresAt = a.ExpiresAt,
                    IsActive = a.IsActive
                })
                .ToListAsync();
        }

        public async Task<SearchResultDto> SearchAsync(SearchQueryDto dto)
        {
            return await _searchService.SearchAsync(dto);
        }

        public async Task<int> SyncAllToSearchAsync()
        {
            var all = await _context.Announcements
                .Include(a => a.VehicleDetails)
                .Include(a => a.Photos)
                .ToListAsync();

            await _searchService.IndexManyAnnouncementsAsync(all);
            return all.Count;
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var announcement = await _context.Announcements
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) throw new KeyNotFoundException("Nie znaleziono ogłoszenia.");
            if (announcement.UserId != userId) throw new UnauthorizedAccessException("Brak uprawnień.");

            if (announcement.Photos != null)
            {
                foreach (var photo in announcement.Photos)
                {
                    _fileService.DeleteFile(photo.PhotoUrl);
                }
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();
            await _searchService.RemoveAsync(id.ToString());

            await _logService.LogAsync("DELETE_ANNOUNCEMENT", $"Usunięto ogłoszenie ID: {id}", userId.ToString());
        }

        public async Task RenewAsync(int id, int userId)
        {
            var announcement = await _context.Announcements
                .Include(a => a.VehicleDetails)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) throw new KeyNotFoundException();
            if (announcement.UserId != userId) throw new UnauthorizedAccessException();

            var timeLeft = announcement.ExpiresAt - DateTime.UtcNow;

            if (announcement.IsActive && announcement.ExpiresAt > DateTime.UtcNow && timeLeft.TotalDays > 7)
            {
                throw new InvalidOperationException("Ogłoszenie można przedłużyć tylko, gdy zostanie mniej niż 7 dni.");
            }

            announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            announcement.IsActive = true;

            await _context.SaveChangesAsync();
            await _searchService.IndexAnnouncementAsync(announcement);
        }

        public async Task ActivateAsync(int id, int userId)
        {
            var announcement = await _context.Announcements
                .Include(a => a.VehicleDetails)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) throw new KeyNotFoundException();
            if (announcement.UserId != userId) throw new UnauthorizedAccessException();

            announcement.IsActive = true;
            if (announcement.ExpiresAt < DateTime.UtcNow)
            {
                announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            }

            await _context.SaveChangesAsync();
            await _searchService.IndexAnnouncementAsync(announcement);
        }

        public async Task UpdateAsync(int id, CreateAnnouncementDto dto, int userId)
        {
            var moderation = await _aiModerationService.CheckContentAsync(dto.Title, dto.Description, dto.Price);
            if (!moderation.IsSafe)
            {
                await _logService.LogAsync("AI_BLOCK_EDIT", $"Zablokowano edycję ID {id}: {dto.Title}. Powód: {moderation.Reason}", userId.ToString());
                throw new InvalidOperationException($"Edycja zablokowana przez AI. Powód: {moderation.Reason}");
            }

            var announcement = await _context.Announcements
                .Include(a => a.VehicleDetails)
                .Include(a => a.PartDetails)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) throw new KeyNotFoundException();
            if (announcement.UserId != userId) throw new UnauthorizedAccessException();

            announcement.Title = dto.Title;
            announcement.Description = dto.Description;
            announcement.Price = dto.Price;
            announcement.Category = dto.Category;
            announcement.Location = dto.Location;
            announcement.Latitude = ParseCoordinate(dto.Latitude);
            announcement.Longitude = ParseCoordinate(dto.Longitude);
            announcement.PhoneNumber = dto.PhoneNumber;
            announcement.ContactPreference = dto.ContactPreference;

            if (dto.Photos != null && dto.Photos.Count > 0)
            {
                foreach (var oldPhoto in announcement.Photos)
                {
                    _fileService.DeleteFile(oldPhoto.PhotoUrl);
                }
                announcement.Photos.Clear();
                bool isFirst = true;
                foreach (var file in dto.Photos)
                {
                    string path = await _fileService.SaveFileAsync(file);
                    var photo = new AnnouncementPhoto { PhotoUrl = path, IsMain = isFirst };
                    announcement.Photos.Add(photo);

                    if (isFirst) { announcement.PhotoUrl = path; isFirst = false; }
                }
            }

            if (dto.Category == "Pojazd" && dto.VehicleDetails != null)
            {
                if (announcement.VehicleDetails == null) announcement.VehicleDetails = new VehicleDetails();

                var v = announcement.VehicleDetails;
                v.Brand = dto.VehicleDetails.Brand;
                v.Model = dto.VehicleDetails.Model;
                v.Generation = dto.VehicleDetails.Generation;
                v.Year = dto.VehicleDetails.Year;
                v.Mileage = dto.VehicleDetails.Mileage;
                v.EnginePower = dto.VehicleDetails.EnginePower;
                v.EngineCapacity = dto.VehicleDetails.EngineCapacity;
                v.FuelType = dto.VehicleDetails.FuelType;
                v.Gearbox = dto.VehicleDetails.Gearbox;
                v.BodyType = dto.VehicleDetails.BodyType;
                v.DriveType = dto.VehicleDetails.DriveType;
                v.Color = dto.VehicleDetails.Color;
                v.VIN = dto.VehicleDetails.VIN;
                v.State = dto.VehicleDetails.State;
            }

            if (dto.Category == "Część" && dto.PartDetails != null)
            {
                if (announcement.PartDetails == null) announcement.PartDetails = new PartDetails();

                var p = announcement.PartDetails;
                p.PartName = dto.PartDetails.PartName;
                p.PartNumber = dto.PartDetails.PartNumber;
                p.Compatibility = dto.PartDetails.Compatibility;
                p.State = dto.PartDetails.State;
            }

            await _context.SaveChangesAsync();
            await _searchService.IndexAnnouncementAsync(announcement);

            await _logService.LogAsync("UPDATE_ANNOUNCEMENT", $"Zaktualizowano ogłoszenie ID: {id}", userId.ToString());
        }
    }
}