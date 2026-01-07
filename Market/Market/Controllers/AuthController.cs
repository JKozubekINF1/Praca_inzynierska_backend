using Market.DTOs;
using Market.Services;
using Market.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new { Message = result.Message });
            }

            return Ok(new { Message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);

            if (token == null)
            {
                return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
            }

            SetTokenCookie(token);

            return Ok(new { Message = "Zalogowano pomyślnie." });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return Ok(new { Message = "Wylogowano pomyślnie." });
        }

        [HttpGet("verify")]
        public IActionResult Verify()
        {
            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { Message = "Brak tokenu." });
            }

            try
            {
                var userDto = _authService.Verify(token);
                return Ok(new { Message = "Token ważny", User = userDto });
            }
            catch (Exception)
            {
                return Unauthorized(new { Message = "Nieprawidłowy lub wygasły token." });
            }
        }

        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            };
            Response.Cookies.Append("AuthToken", token, cookieOptions);
        }
    }
}