using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Market.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Market.Tests.Services
{
    public class UserServiceTests
    {
        private readonly AppDbContext _context;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _service = new UserService(_context);
        }

        private User CreateUser(int id, string username, string email, string plainPassword)
        {
            return new User
            {
                Id = id,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = "User",
                IsBanned = false,
                PhoneNumber = "123456789",
                HasCompletedProfilePrompt = false
            };
        }


        [Fact]
        public async Task GetProfileAsync_ShouldReturnUserData_WhenUserExists()
        {
            var user = CreateUser(1, "Janusz", "j@j.pl", "pass");
            user.Name = "Jan";
            user.Surname = "Kowalski";

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _service.GetProfileAsync(1);

            result.Should().NotBeNull();
            result.Username.Should().Be("Janusz");
            result.Name.Should().Be("Jan");
            result.Surname.Should().Be("Kowalski");
        }

        [Fact]
        public async Task GetProfileAsync_ShouldThrow_WhenUserNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetProfileAsync(999));
        }


        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateFields_WhenDataIsValid()
        {
            var user = CreateUser(1, "OldName", "old@test.com", "pass");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new UserDto
            {
                Username = "NewName",
                Email = "new@test.com",
                Name = "Marek",
                Surname = "Nowak",
                PhoneNumber = "123456789"
            };

            await _service.UpdateProfileAsync(1, dto);

            var dbUser = await _context.Users.FindAsync(1);
            dbUser.Username.Should().Be("NewName");
            dbUser.Email.Should().Be("new@test.com");
            dbUser.Name.Should().Be("Marek");
            dbUser.HasCompletedProfilePrompt.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldThrow_WhenEmailIsTakenByOtherUser()
        {
            var u1 = CreateUser(1, "User1", "zajety@test.com", "pass");
            var u2 = CreateUser(2, "User2", "moj@test.com", "pass");

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var dto = new UserDto
            {
                Username = "User2",
                Email = "zajety@test.com",
                PhoneNumber = "123456789"
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateProfileAsync(2, dto));

            ex.Message.Should().Contain("Email jest już używany");
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldThrow_WhenPhoneNumberIsInvalid()
        {
            var user = CreateUser(1, "User", "u@u.pl", "pass");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new UserDto
            {
                Username = "User",
                Email = "u@u.pl",
                PhoneNumber = "ZlyNumer"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateProfileAsync(1, dto));

            ex.Message.Should().Contain("poprawny numer telefonu");
        }


        [Fact]
        public async Task ChangePasswordAsync_ShouldSucceed_WhenOldPasswordIsCorrect_AndNewIsStrong()
        {
            string currentPass = "StareHaslo1!";
            var user = CreateUser(1, "User", "u@u.pl", currentPass);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new ChangePasswordDto
            {
                CurrentPassword = currentPass,
                NewPassword = "NoweSilneHaslo1!"
            };

            await _service.ChangePasswordAsync(1, dto);

            var dbUser = await _context.Users.FindAsync(1);

            bool oldWorks = BCrypt.Net.BCrypt.Verify(currentPass, dbUser.PasswordHash);
            oldWorks.Should().BeFalse();

            bool newWorks = BCrypt.Net.BCrypt.Verify(dto.NewPassword, dbUser.PasswordHash);
            newWorks.Should().BeTrue();
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIsWrong()
        {
            var user = CreateUser(1, "User", "u@u.pl", "DobreHaslo1!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "ZleHaslo",
                NewPassword = "NoweHaslo1!"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ChangePasswordAsync(1, dto));

            ex.Message.Should().Contain("nieprawidłowe");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WhenNewPasswordIsWeak()
        {
            var user = CreateUser(1, "User", "u@u.pl", "Stare1!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Stare1!",
                NewPassword = "slabe"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ChangePasswordAsync(1, dto));

            ex.Message.Should().NotBeNullOrEmpty();
        }
    }
}