using Market.Data;
using Market.DTOs;
using Market.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Market.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RecaptchaService _recaptchaService;

        public AuthService(AppDbContext context, IConfiguration configuration, RecaptchaService recaptchaService)
        {
            _context = context;
            _configuration = configuration;
            _recaptchaService = recaptchaService;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
        {
            var isCaptchaValid = await _recaptchaService.VerifyTokenAsync(dto.RecaptchaToken);
            if (!isCaptchaValid)
                return (false, "Weryfikacja Captcha nie powiodła się. Jesteś robotem?");

            if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 8)
                return (false, "Hasło musi mieć co najmniej 8 znaków.");

            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};:'"",.<>?]).+$";
            if (!Regex.IsMatch(dto.Password, passwordPattern))
                return (false, "Hasło musi zawierać wielką literę i znak specjalny.");

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
                return (false, "Użytkownik o podanej nazwie lub emailu już istnieje.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsBanned = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Rejestracja zakończona sukcesem.");
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var isCaptchaValid = await _recaptchaService.VerifyTokenAsync(dto.RecaptchaToken);
            if (!isCaptchaValid)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;


            if (user.IsBanned)
            {
                
                throw new UnauthorizedAccessException("Twoje konto zostało zablokowane. Skontaktuj się z administracją.");
            }
            

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("Brak klucza JWT w konfiguracji.");

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}