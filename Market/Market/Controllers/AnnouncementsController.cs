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

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromForm] CreateAnnouncementDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized("Nie jesteś zalogowany.");

            if (dto.Price <= 0) return BadRequest("Cena musi być większa od 0.");

            try
            {
                var userId = int.Parse(userIdString);
                var id = await _announcementService.CreateAsync(dto, userId);

                return CreatedAtAction(nameof(GetAnnouncement), new { id = id }, new { Message = "Ogłoszenie dodane pomyślnie.", AnnouncementId = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Wystąpił błąd wewnętrzny serwera.");
            }
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var list = await _announcementService.GetUserAnnouncementsAsync(userId);
            return Ok(list);
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> Renew(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _announcementService.RenewAsync(id, userId);
                return Ok(new { Message = "Ogłoszenie przedłużone o 30 dni." });
            }
            catch (InvalidOperationException ex) 
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _announcementService.ActivateAsync(id, userId);
                return Ok(new { Message = "Ogłoszenie aktywowane." });
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
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
            try
            {
                var count = await _announcementService.SyncAllToSearchAsync();
                return Ok(new { Message = $"Zsynchronizowano {count} ogłoszeń z Algolią." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd synchronizacji: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _announcementService.DeleteAsync(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Nie znaleziono ogłoszenia.");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid(); 
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateAnnouncementDto dto)
        {

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _announcementService.UpdateAsync(id, dto, userId);
                return Ok(new { Message = "Ogłoszenie zaktualizowane." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}