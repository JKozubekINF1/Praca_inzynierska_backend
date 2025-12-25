using Market.Constants;
using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;
        private readonly ISearchService _searchService;
        private readonly IFileService _fileService; 

        public AnnouncementService(AppDbContext context, ISearchService searchService, IFileService fileService)
        {
            _context = context;
            _searchService = searchService;
            _fileService = fileService;
        }

        public async Task<int> CreateAsync(CreateAnnouncementDto dto, int userId)
        {
            var announcement = new Announcement
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Category = dto.Category,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                PhoneNumber = dto.PhoneNumber,
                ContactPreference = dto.ContactPreference,
                Location = dto.Location,
                Features = dto.Features.Select(f => new AnnouncementFeature { FeatureName = f }).ToList(),
                Photos = new List<AnnouncementPhoto>() 
            };
            if (dto.Photos != null && dto.Photos.Count > 0)
            {
                foreach (var file in dto.Photos)
                {
                    if (file.Length > 0)
                    {
                        var photoUrl = await _fileService.SaveFileAsync(file);
                        announcement.Photos.Add(new AnnouncementPhoto
                        {
                            PhotoUrl = photoUrl,
                            IsMain = announcement.Photos.Count == 0
                        });
                    }
                }
            }

            if (dto.Category == CategoryConstants.Vehicle && dto.VehicleDetails != null)
            {
                announcement.VehicleDetails = MapVehicleDetails(dto.VehicleDetails);
            }
            else if (dto.Category == CategoryConstants.Part && dto.PartDetails != null)
            {
                announcement.PartDetails = MapPartDetails(dto.PartDetails);
            }
            else
            {
                throw new ArgumentException("Nieprawidłowa kategoria lub brak szczegółów.");
            }
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            await _searchService.IndexAnnouncementAsync(announcement);

            return announcement.Id;
        }

        public async Task<Announcement?> GetByIdAsync(int id)
        {
            return await _context.Announcements
                .Include(a => a.User)
                .Include(a => a.VehicleDetails)
                .Include(a => a.PartDetails)
                .Include(a => a.Features)
                .Include(a => a.Photos) 
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        private VehicleDetails MapVehicleDetails(VehicleDetailsDto dto)
        {
            return new VehicleDetails
            {
                Brand = dto.Brand,
                Model = dto.Model,
                Generation = dto.Generation,
                Year = dto.Year,
                Mileage = dto.Mileage,
                EnginePower = dto.EnginePower,
                EngineCapacity = dto.EngineCapacity,
                FuelType = dto.FuelType,
                Gearbox = dto.Gearbox,
                BodyType = dto.BodyType,
                DriveType = dto.DriveType,
                Color = dto.Color,
                VIN = dto.VIN,
                State = dto.State
            };
        }

        private PartDetails MapPartDetails(PartDetailsDto dto)
        {
            return new PartDetails
            {
                PartName = dto.PartName,
                PartNumber = dto.PartNumber,
                Compatibility = dto.Compatibility,
                State = dto.State
            };
        }

        public async Task<IEnumerable<AnnouncementListDto>> GetUserAnnouncementsAsync(int userId)
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
                    CreatedAt = a.CreatedAt,
                    ExpiresAt = a.ExpiresAt,
                    IsActive = a.ExpiresAt >= DateTime.UtcNow
                    // Opcjonalnie: ThumbnailUrl = a.Photos.FirstOrDefault(p => p.IsMain).PhotoUrl
                })
                .ToListAsync();
        }

        public async Task RenewAsync(int id, int userId)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) throw new KeyNotFoundException("Ogłoszenie nie istnieje.");
            if (announcement.UserId != userId) throw new UnauthorizedAccessException("Nie jesteś właścicielem.");

            announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            await _context.SaveChangesAsync();
        }

        public async Task ActivateAsync(int id, int userId)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) throw new KeyNotFoundException("Ogłoszenie nie istnieje.");
            if (announcement.UserId != userId) throw new UnauthorizedAccessException("Nie jesteś właścicielem.");

            if (announcement.ExpiresAt < DateTime.UtcNow)
                announcement.ExpiresAt = DateTime.UtcNow.AddDays(30);
            else
                announcement.ExpiresAt = announcement.ExpiresAt.AddDays(30);

            await _context.SaveChangesAsync();
        }

        public async Task<SearchResultDto> SearchAsync(SearchQueryDto query)
        {
            return await _searchService.SearchAsync(query);
        }

        public async Task<int> SyncAllToSearchAsync()
        {
            var allAnnouncements = await _context.Announcements
                .Include(a => a.VehicleDetails)
                .Include(a => a.PartDetails)
                // .Include(a => a.Photos) 
                .ToListAsync();

            await _searchService.IndexManyAnnouncementsAsync(allAnnouncements);

            return allAnnouncements.Count;
        }
    }
}