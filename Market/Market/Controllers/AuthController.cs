using Market.DTOs;
using Market.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
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
            try
            {
                var token = await _authService.LoginAsync(dto);

                if (token == null)
                {
                    return Unauthorized(new { Message = "Nieprawidłowa nazwa użytkownika lub hasło." });
                }

                Response.Cookies.Append("AuthToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });

                return Ok(new { Message = "Zalogowano pomyślnie." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Wystąpił błąd serwera." });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("AuthToken", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });
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
                var key = _configuration["Jwt:Key"];
                var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = symmetricKey
                }, out SecurityToken validatedToken);

                var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                var username = principal.Identity?.Name;
                var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdString, out int userId);

                return Ok(new
                {
                    Message = "Token ważny",
                    User = new { Id = userId, Username = username, Role = role }
                });
            }
            catch
            {
                return Unauthorized(new { Message = "Nieprawidłowy lub wygasły token." });
            }
        }
    }
}