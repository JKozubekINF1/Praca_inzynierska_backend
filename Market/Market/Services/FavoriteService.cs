using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly AppDbContext _context;

        public FavoriteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FavoriteResponseDto> ToggleFavoriteAsync(int userId, int announcementId)
        {
            var exists = await _context.Announcements.AnyAsync(a => a.Id == announcementId);
            if (!exists)
            {
                throw new KeyNotFoundException("Ogłoszenie nie istnieje.");
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.AnnouncementId == announcementId);

            if (existingFavorite != null)
            {
                _context.Favorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return new FavoriteResponseDto { IsFavorite = false, Message = "Usunięto z ulubionych." };
            }
            else
            {
                var favorite = new Favorite
                {
                    UserId = userId,
                    AnnouncementId = announcementId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return new FavoriteResponseDto { IsFavorite = true, Message = "Dodano do ulubionych." };
            }
        }

        public async Task<List<AnnouncementListDto>> GetUserFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Include(f => f.Announcement)
                .ThenInclude(a => a.Photos)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new AnnouncementListDto
                {
                    Id = f.Announcement.Id,
                    Title = f.Announcement.Title,
                    Price = f.Announcement.Price,
                    Category = f.Announcement.Category,
                    Location = f.Announcement.Location,
                    PhotoUrl = f.Announcement.PhotoUrl,
                    CreatedAt = f.Announcement.CreatedAt,
                    ExpiresAt = f.Announcement.ExpiresAt,
                    IsActive = f.Announcement.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<int>> GetUserFavoriteIdsAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.AnnouncementId)
                .ToListAsync();
        }
    }
}