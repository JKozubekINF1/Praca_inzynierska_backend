using Market.Data;
using Market.DTOs;      
using Market.Helpers;   
using Market.Interfaces;
using Market.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private readonly ISearchService _searchService;

        public AdminController(
            AppDbContext context,
            IFileService fileService,
            ILogService logService,
            ISearchService searchService)
        {
            _context = context;
            _fileService = fileService;
            _logService = logService;
            _searchService = searchService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams pagination)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
            {
                var term = pagination.Search.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
            }

            var totalItems = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsBanned,
                    AnnouncementCount = u.Announcements != null ? u.Announcements.Count : 0
                })
                .ToListAsync();

            var result = new PagedResult<object>(
                users.Cast<object>().ToList(),
                totalItems,
                pagination.PageNumber,
                pagination.PageSize);

            return Ok(result);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Announcements)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound("Użytkownik nie istnieje.");

            if (user.Role == "Admin")
            {
                return BadRequest("Nie można usunąć Administratora.");
            }

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

            string adminName = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync("DELETE_USER", $"Usunięto użytkownika: {user.Username}", adminName);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Użytkownik został usunięty." });
        }


        [HttpGet("announcements")]
        public async Task<IActionResult> GetAllAnnouncements([FromQuery] PaginationParams pagination)
        {
            var query = _context.Announcements.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
            {
                var term = pagination.Search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(term) || a.User.Username.ToLower().Contains(term));
            }

            var totalItems = await query.CountAsync();

            var announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Price,
                    a.CreatedAt,
                    a.IsActive,
                    Author = a.User.Username,
                    a.PhotoUrl
                })
                .ToListAsync();

            var result = new PagedResult<object>(
                announcements.Cast<object>().ToList(),
                totalItems,
                pagination.PageNumber,
                pagination.PageSize);

            return Ok(result);
        }

        [HttpDelete("announcements/{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) return NotFound("Ogłoszenie nie istnieje.");

            if (!string.IsNullOrEmpty(announcement.PhotoUrl))
            {
                _fileService.DeleteFile(announcement.PhotoUrl);
            }

            string adminName = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync("DELETE_ANNOUNCEMENT_ADMIN", $"Admin usunął ogłoszenie ID {id}: {announcement.Title}", adminName);

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            try
            {
                await _searchService.RemoveAsync(id.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd usuwania z Algolii: {ex.Message}");
            }

            return Ok(new { Message = "Ogłoszenie zostało usunięte." });
        }


        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.SystemLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Announcements)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound("Nie znaleziono użytkownika.");

            var result = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.IsBanned,
                user.PhoneNumber,
                Announcements = user.Announcements.Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Price,
                    a.CreatedAt,
                    a.IsActive,
                    a.PhotoUrl
                })
            };

            return Ok(result);
        }

        [HttpPost("users/{id}/toggle-ban")]
        public async Task<IActionResult> ToggleBan(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Nie znaleziono użytkownika.");

            if (user.Role == "Admin")
            {
                return BadRequest("Nie można zbanować administratora.");
            }

            user.IsBanned = !user.IsBanned;

            await _context.SaveChangesAsync();

            string adminName = User.Identity?.Name ?? "Admin";
            string action = user.IsBanned ? "USER_BAN" : "USER_UNBAN";
            string message = user.IsBanned
                ? $"Zbanowano użytkownika: {user.Username}"
                : $"Odbanowano użytkownika: {user.Username}";

            await _logService.LogAsync(action, message, adminName);

            return Ok(new
            {
                Message = message,
                IsBanned = user.IsBanned
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalAnnouncements = await _context.Announcements.CountAsync();

            var newAnnouncementsToday = await _context.Announcements
                .Where(a => a.CreatedAt >= DateTime.Today)
                .CountAsync();

            var recentLogs = await _context.SystemLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .Select(l => new { l.Action, l.Message, l.CreatedAt, l.Username })
                .ToListAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalAnnouncements = totalAnnouncements,
                NewAnnouncementsToday = newAnnouncementsToday,
                RecentLogs = recentLogs
            });
        }
    }
}