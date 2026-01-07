using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Market.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
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

            if (user == null) throw new KeyNotFoundException("Użytkownik nie znaleziony.");

            return user;
        }

        public async Task UpdateProfileAsync(int userId, UserDto dto)
        {
            if (string.IsNullOrEmpty(dto.Username) || dto.Username.Length > 100)
                throw new ArgumentException("Nazwa użytkownika jest wymagana i nie może przekraczać 100 znaków.");

            if (string.IsNullOrEmpty(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
                throw new ArgumentException("Poprawny adres email jest wymagany.");

            if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Length > 100)
                throw new ArgumentException("Imię nie może przekraczać 100 znaków.");

            if (!string.IsNullOrEmpty(dto.Surname) && dto.Surname.Length > 100)
                throw new ArgumentException("Nazwisko nie może przekraczać 100 znaków.");

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                if (dto.PhoneNumber.Length > 15 || !Regex.IsMatch(dto.PhoneNumber, @"^\+?\d{9,15}$"))
                    throw new ArgumentException("Podaj poprawny numer telefonu (9-15 cyfr).");
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
                throw new InvalidOperationException("Email jest już używany przez innego użytkownika.");

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != userId))
                throw new InvalidOperationException("Nazwa użytkownika jest już używana.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Użytkownik nie znaleziony.");

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.PhoneNumber = dto.PhoneNumber;
            user.HasCompletedProfilePrompt = true;

            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Użytkownik nie znaleziony.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new ArgumentException("Aktualne hasło jest nieprawidłowe.");

            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 8)
                throw new ArgumentException("Nowe hasło musi mieć co najmniej 8 znaków.");

            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};:'"",.<>?]).+$";
            if (!Regex.IsMatch(dto.NewPassword, passwordPattern))
                throw new ArgumentException("Nowe hasło musi zawierać co najmniej jedną wielką literę i jeden znak specjalny.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
        }
    }
}