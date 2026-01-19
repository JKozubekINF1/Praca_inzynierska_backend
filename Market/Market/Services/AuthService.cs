using Market.Data;
using Market.DTOs;
using Market.Models;
using Market.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using BCrypt.Net;

namespace Market.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RecaptchaService _recaptchaService;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IConfiguration configuration, RecaptchaService recaptchaService, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _recaptchaService = recaptchaService;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
        {
            var isCaptchaValid = await _recaptchaService.VerifyTokenAsync(dto.RecaptchaToken);
            if (!isCaptchaValid)
                return (false, "Weryfikacja Captcha nie powiodła się.");

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
            if (!isCaptchaValid) return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            if (user.IsBanned)
                throw new UnauthorizedAccessException("Twoje konto zostało zablokowane. Skontaktuj się z administracją.");

            return GenerateJwtToken(user);
        }

        public VerifyResultDto Verify(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
                var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;
                var role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? "User";

                return new VerifyResultDto
                {
                    Id = userId,
                    Username = username,
                    Role = role
                };
            }
            catch
            {
                throw new SecurityTokenException("Invalid token");
            }
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (true, "Jeśli konto istnieje, wysłano kod weryfikacyjny.");
            }

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            user.PasswordResetToken = code;
            user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(15); 

            await _context.SaveChangesAsync();

            var message = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Reset hasła</h2>
                    <p>Twój kod weryfikacyjny to:</p>
                    <h1 style='background-color: #f0f0f0; padding: 10px; display: inline-block; letter-spacing: 5px;'>{code}</h1>
                    <p>Kod jest ważny przez 15 minut.</p>
                    <p>Jeśli to nie Ty prosiłeś o reset hasła, zignoruj tę wiadomość.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Kod resetu hasła - Market", message);
                return (true, "Jeśli konto istnieje, wysłano kod weryfikacyjny.");
            }
            catch (Exception ex)
            {
                return (false, $"BŁĄD SMTP: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return (false, "Nieprawidłowy adres email.");

            if (user.PasswordResetToken != dto.Code || user.ResetTokenExpires < DateTime.UtcNow)
                return (false, "Nieprawidłowy lub wygasły kod weryfikacyjny.");

            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};:'"",.<>?]).+$";
            if (!Regex.IsMatch(dto.NewPassword, passwordPattern))
                return (false, "Hasło musi zawierać wielką literę i znak specjalny.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync();

            return (true, "Hasło zostało zmienione pomyślnie.");
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