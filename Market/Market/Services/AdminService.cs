using Market.Data;
using Market.DTOs;
using Market.DTOs.Admin;
using Market.Helpers;
using Market.Interfaces; 
using Market.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private readonly ISearchService _searchService;

        public AdminService(AppDbContext context, IFileService fileService, ILogService logService, ISearchService searchService)
        {
            _context = context;
            _fileService = fileService;
            _logService = logService;
            _searchService = searchService;
        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(PaginationParams pagination)
        {
            var query = _context.Users.Include(u => u.Announcements).AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
            {
                var term = pagination.Search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.Id)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsBanned = u.IsBanned,
                    AnnouncementCount = u.Announcements.Count
                })
                .ToListAsync();

            return new PagedResult<AdminUserDto>(items, totalItems, pagination.PageNumber, pagination.PageSize);
        }

        public async Task DeleteUserAsync(int id, string adminName)
        {
            var user = await _context.Users
                .Include(u => u.Announcements)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) throw new KeyNotFoundException("Użytkownik nie istnieje.");
            if (user.Role == "Admin") throw new InvalidOperationException("Nie można usunąć Administratora.");
            if (user.Announcements != null)
            {
                foreach (var announcement in user.Announcements)
                {
                    if (!string.IsNullOrEmpty(announcement.PhotoUrl))
                    {
                        _fileService.DeleteFile(announcement.PhotoUrl);
                    }
                    try
                    {
                        await _searchService.RemoveAsync(announcement.Id.ToString());
                    }
                    catch
                    {
                    }
                }
            }

            _context.Users.Remove(user);
            await _logService.LogAsync("DELETE_USER", $"Usunięto użytkownika: {user.Username}", adminName);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<AdminAnnouncementDto>> GetAnnouncementsAsync(PaginationParams pagination)
        {
            var query = _context.Announcements.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
            {
                var term = pagination.Search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term) || a.User.Username.ToLower().Contains(term));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(a => new AdminAnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Price = a.Price,
                    CreatedAt = a.CreatedAt,
                    IsActive = a.IsActive,
                    Author = a.User.Username,
                    PhotoUrl = a.PhotoUrl
                })
                .ToListAsync();

            return new PagedResult<AdminAnnouncementDto>(items, totalItems, pagination.PageNumber, pagination.PageSize);
        }

        public async Task DeleteAnnouncementAsync(int id, string adminName)
        {
            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
            if (announcement == null) throw new KeyNotFoundException("Ogłoszenie nie istnieje.");

            if (!string.IsNullOrEmpty(announcement.PhotoUrl))
            {
                _fileService.DeleteFile(announcement.PhotoUrl);
            }

            _context.Announcements.Remove(announcement);
            await _logService.LogAsync("DELETE_ANNOUNCEMENT_ADMIN", $"Admin usunął ogłoszenie ID {id}: {announcement.Title}", adminName);
            await _context.SaveChangesAsync();

            try
            {
                await _searchService.RemoveAsync(id.ToString());
            }
            catch
            {
            }
        }

        public async Task<List<SystemLog>> GetLogsAsync()
        {
            return await _context.SystemLogs.OrderByDescending(l => l.CreatedAt).Take(100).ToListAsync();
        }

        public async Task<object> GetUserDetailAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Announcements)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) throw new KeyNotFoundException("Nie znaleziono użytkownika.");

            return new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsBanned,
                user.PhoneNumber,
                Announcements = user.Announcements.Select(a => new { a.Id, a.Title, a.Price, a.CreatedAt, a.IsActive, a.PhotoUrl })
            };
        }

        public async Task<bool> ToggleBanAsync(int id, string adminName)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) throw new KeyNotFoundException("Nie znaleziono użytkownika.");
            if (user.Role == "Admin") throw new InvalidOperationException("Nie można zbanować administratora.");

            user.IsBanned = !user.IsBanned;
            await _context.SaveChangesAsync();

            string action = user.IsBanned ? "USER_BAN" : "USER_UNBAN";
            string message = user.IsBanned ? $"Zbanowano użytkownika: {user.Username}" : $"Odbanowano użytkownika: {user.Username}";

            await _logService.LogAsync(action, message, adminName);

            return user.IsBanned;
        }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalAnnouncements = await _context.Announcements.CountAsync();
            var newToday = await _context.Announcements.Where(a => a.CreatedAt >= DateTime.Today).CountAsync();
            var recentLogs = await _context.SystemLogs.OrderByDescending(l => l.CreatedAt).Take(5).ToListAsync();

            return new AdminStatsDto
            {
                TotalUsers = totalUsers,
                TotalAnnouncements = totalAnnouncements,
                NewAnnouncementsToday = newToday,
                RecentLogs = recentLogs
            };
        }

    }
}