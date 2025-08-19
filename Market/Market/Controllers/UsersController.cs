using Market.Data;
using Market.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Market.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Nie jesteś zalogowany.");
            }

            var user = await _context.Users
                .Where(u => u.Id == int.Parse(userId))
                .Select(u => new UserDto
                {
                    Username = u.Username,
                    Email = u.Email,
                    Name = u.Name,
                    Surname = u.Surname,
                    PhoneNumber = u.PhoneNumber,
                    HasCompletedProfilePrompt = u.HasCompletedProfilePrompt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound("Użytkownik nie znaleziony.");
            }

            return Ok(user);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Nie jesteś zalogowany.");
            }

            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrEmpty(dto.Username) || dto.Username.Length > 100)
            {
                errors.Add("username", new[] { "Nazwa użytkownika jest wymagana i nie może przekraczać 100 znaków." });
            }
            if (string.IsNullOrEmpty(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
            {
                errors.Add("email", new[] { "Poprawny adres email jest wymagany." });
            }
            if (_context.Users.Any(u => u.Email == dto.Email && u.Id != int.Parse(userId)))
            {
                errors.Add("email", new[] { "Email jest już używany przez innego użytkownika." });
            }
            if (_context.Users.Any(u => u.Username == dto.Username && u.Id != int.Parse(userId)))
            {
                errors.Add("username", new[] { "Nazwa użytkownika jest już używana." });
            }
            if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Length > 100)
            {
                errors.Add("name", new[] { "Imię nie może przekraczać 100 znaków." });
            }
            if (!string.IsNullOrEmpty(dto.Surname) && dto.Surname.Length > 100)
            {
                errors.Add("surname", new[] { "Nazwisko nie może przekraczać 100 znaków." });
            }
            if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber.Length > 15)
            {
                errors.Add("phoneNumber", new[] { "Numer telefonu nie może przekraczać 15 znaków." });
            }
            if (!string.IsNullOrEmpty(dto.PhoneNumber) && !Regex.IsMatch(dto.PhoneNumber, @"^\+?\d{9,15}$"))
            {
                errors.Add("phoneNumber", new[] { "Podaj poprawny numer telefonu (9-15 cyfr, może zaczynać się od '+')." });
            }

            if (errors.Count > 0)
            {
                return BadRequest(new
                {
                    Status = 400,
                    Errors = errors,
                    Title = "One or more validation errors occurred."
                });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return NotFound("Użytkownik nie znaleziony.");
            }

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.PhoneNumber = dto.PhoneNumber;
            user.HasCompletedProfilePrompt = true;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profil zaktualizowany pomyślnie." });
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Nie jesteś zalogowany.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
            {
                return NotFound("Użytkownik nie znaleziony.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Aktualne hasło jest nieprawidłowe.");
            }

            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};:'"",.<>?]).+$";
            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 8)
            {
                return BadRequest("Nowe hasło musi mieć co najmniej 8 znaków.");
            }
            if (!Regex.IsMatch(dto.NewPassword, passwordPattern))
            {
                return BadRequest("Nowe hasło musi zawierać co najmniej jedną wielką literę i jeden znak specjalny.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Hasło zmienione pomyślnie." });
        }

        [HttpGet("me/announcements")]
        public async Task<IActionResult> GetUserAnnouncements()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Nie jesteś zalogowany.");
            }

            var announcements = await _context.Announcements
                .Where(a => a.UserId == int.Parse(userId))
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Price,
                    a.Category,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(announcements);
        }
    }
}