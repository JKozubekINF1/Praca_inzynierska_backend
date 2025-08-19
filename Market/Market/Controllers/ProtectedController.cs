using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProtectedController : ControllerBase
    {
        [HttpGet("test")]
        [Authorize]
        public IActionResult Test()
        {
            var username = User.Identity?.Name ?? "Nieznany użytkownik";
            return Ok($"Witaj, {username}! To jest zabezpieczony endpoint w projekcie Market.");
        }
    }
}