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
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }
        private int CurrentUserId
        {
            get
            {
                var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return idStr != null ? int.Parse(idStr) : 0;
            }
        }

        [HttpPost("{announcementId}")]
        public async Task<IActionResult> ToggleFavorite(int announcementId)
        {
            try
            {
                var result = await _favoriteService.ToggleFavoriteAsync(CurrentUserId, announcementId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var result = await _favoriteService.GetUserFavoritesAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpGet("ids")]
        public async Task<IActionResult> GetFavoriteIds()
        {
            var result = await _favoriteService.GetUserFavoriteIdsAsync(CurrentUserId);
            return Ok(result);
        }
    }
}