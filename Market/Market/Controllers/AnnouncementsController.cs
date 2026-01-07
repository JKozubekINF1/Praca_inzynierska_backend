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
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }
        private int GetUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out int id) ? id : 0;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromForm] CreateAnnouncementDto dto)
        {
            if (dto.Price <= 0)
                throw new ArgumentException("Cena musi być większa od 0.");

            var userId = GetUserId();
            var id = await _announcementService.CreateAsync(dto, userId);

            return CreatedAtAction(nameof(GetAnnouncement), new { id = id }, new { Message = "Ogłoszenie dodane pomyślnie.", AnnouncementId = id });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAnnouncement(int id)
        {
            var announcement = await _announcementService.GetByIdAsync(id);
            if (announcement == null) return NotFound("Nie znaleziono ogłoszenia.");

            return Ok(announcement);
        }

        [HttpGet("user/me/announcements")]
        public async Task<IActionResult> GetUserAnnouncements()
        {
            var list = await _announcementService.GetUserAnnouncementsAsync(GetUserId());
            return Ok(list);
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> Renew(int id)
        {
            await _announcementService.RenewAsync(id, GetUserId());
            return Ok(new { Message = "Ogłoszenie przedłużone o 30 dni." });
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            await _announcementService.ActivateAsync(id, GetUserId());
            return Ok(new { Message = "Ogłoszenie aktywowane." });
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromQuery] SearchQueryDto dto)
        {
            var result = await _announcementService.SearchAsync(dto);
            return Ok(result);
        }

        [HttpPost("sync-algolia")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncAll()
        {
            var count = await _announcementService.SyncAllToSearchAsync();
            return Ok(new { Message = $"Zsynchronizowano {count} ogłoszeń z Algolią." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _announcementService.DeleteAsync(id, GetUserId());
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateAnnouncementDto dto)
        {
            await _announcementService.UpdateAsync(id, dto, GetUserId());
            return Ok(new { Message = "Ogłoszenie zaktualizowane." });
        }
    }
}