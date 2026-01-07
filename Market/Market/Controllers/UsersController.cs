using Market.DTOs;
using Market.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAnnouncementService _announcementService;

        public UsersController(IUserService userService, IAnnouncementService announcementService)
        {
            _userService = userService;
            _announcementService = announcementService;
        }
        private int GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out int id) ? id : 0;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userService.GetProfileAsync(GetUserId());
            return Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDto dto)
        {
            await _userService.UpdateProfileAsync(GetUserId(), dto);
            return Ok(new { Message = "Profil zaktualizowany pomyślnie." });
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _userService.ChangePasswordAsync(GetUserId(), dto);
            return Ok(new { Message = "Hasło zmienione pomyślnie." });
        }

        [HttpGet("me/announcements")]
        public async Task<IActionResult> GetUserAnnouncements()
        {
            var list = await _announcementService.GetUserAnnouncementsAsync(GetUserId());
            return Ok(list);
        }
    }
}