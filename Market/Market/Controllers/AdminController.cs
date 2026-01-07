using Market.DTOs;              
using Market.Interfaces;    
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams pagination)
        {
            var result = await _adminService.GetUsersAsync(pagination);
            return Ok(result);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _adminService.DeleteUserAsync(id, User.Identity?.Name ?? "Admin");
            return Ok(new { Message = "Użytkownik został usunięty." });
        }

        [HttpGet("announcements")]
        public async Task<IActionResult> GetAllAnnouncements([FromQuery] PaginationParams pagination)
        {
            var result = await _adminService.GetAnnouncementsAsync(pagination);
            return Ok(result);
        }

        [HttpDelete("announcements/{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            await _adminService.DeleteAnnouncementAsync(id, User.Identity?.Name ?? "Admin");
            return Ok(new { Message = "Ogłoszenie zostało usunięte." });
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _adminService.GetLogsAsync();
            return Ok(logs);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _adminService.GetUserDetailAsync(id);
            return Ok(user);
        }

        [HttpPost("users/{id}/toggle-ban")]
        public async Task<IActionResult> ToggleBan(int id)
        {
            var isBanned = await _adminService.ToggleBanAsync(id, User.Identity?.Name ?? "Admin");

            return Ok(new
            {
                Message = isBanned ? "Użytkownik zbanowany." : "Użytkownik odbanowany.",
                IsBanned = isBanned
            });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _adminService.GetStatsAsync();
            return Ok(stats);
        }
    }
}