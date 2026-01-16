using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.Models;
using Market.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly RecaptchaService _realRecaptchaService;
        private readonly AuthService _service;

        private const string TestJwtKey = "SuperTajnyKluczDoTestow_MusieMiecodpowiedniaDlugosc123!";

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns(TestJwtKey);
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["Recaptcha:SecretKey"]).Returns("dummy_secret_key");

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
            var httpClient = new HttpClient();

            _realRecaptchaService = new RecaptchaService(
                _mockConfig.Object,
                httpClient,
                _mockEnv.Object
            );

            _service = new AuthService(_context, _mockConfig.Object, _realRecaptchaService);
        }

        private User CreateUser(string username, string email, string password)
        {
            return new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "User",
                IsBanned = false,
                PhoneNumber = "123456"
            };
        }


        [Fact]
        public async Task RegisterAsync_ShouldSucceed_WhenDataIsValid()
        {
            var dto = new RegisterDto
            {
                Username = "NowyUser",
                Email = "test@test.com",
                Password = "Password1!",
                RecaptchaToken = "TEST"
            };

            var result = await _service.RegisterAsync(dto);

            result.Success.Should().BeTrue();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "NowyUser");
            user.Should().NotBeNull();
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenPasswordIsWeak()
        {
            var dto = new RegisterDto
            {
                Username = "UserWeak",
                Email = "weak@test.com",
                Password = "slabe",
                RecaptchaToken = "TEST"
            };

            var result = await _service.RegisterAsync(dto);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("8 znaków");
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenUserAlreadyExists()
        {
            _context.Users.Add(CreateUser("StaryUser", "zajety@email.com", "Haslo1!"));
            await _context.SaveChangesAsync();

            var dto = new RegisterDto
            {
                Username = "InnyLogin",
                Email = "zajety@email.com",
                Password = "Password1!",
                RecaptchaToken = "TEST"
            };

            var result = await _service.RegisterAsync(dto);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("istnieje");
        }

        [Fact]
        public async Task RegisterAsync_ShouldFail_WhenCaptchaIsInvalid()
        {
            var dto = new RegisterDto
            {
                Username = "Bot",
                Password = "Password1!",
                Email = "bot@bot.com",
                RecaptchaToken = "INVALID_TOKEN"
            };

            var result = await _service.RegisterAsync(dto);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Captcha");
        }


        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            string pass = "MySecretPassword1!";
            var user = CreateUser("Logujacy", "log@test.com", pass);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Username = "Logujacy",
                Password = pass,
                RecaptchaToken = "TEST"
            };

            var token = await _service.LoginAsync(loginDto);

            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsWrong()
        {
            var user = CreateUser("UserX", "x@test.com", "DobreHaslo1!");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Username = "UserX",
                Password = "ZleHaslo",
                RecaptchaToken = "TEST"
            };

            var token = await _service.LoginAsync(loginDto);

            token.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenUserIsBanned()
        {
            var user = CreateUser("BannedUser", "ban@test.com", "Pass1!");
            user.IsBanned = true;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Username = "BannedUser",
                Password = "Pass1!",
                RecaptchaToken = "TEST"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.LoginAsync(loginDto));
        }


        [Fact]
        public async Task Verify_ShouldReturnUserData_WhenTokenIsValid()
        {
            var user = CreateUser("TokenUser", "token@test.com", "Pass1!");
            user.Role = "Admin";
            user.Id = 99;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = await _service.LoginAsync(new LoginDto
            {
                Username = "TokenUser",
                Password = "Pass1!",
                RecaptchaToken = "TEST"
            });

            var result = _service.Verify(token);

            result.Should().NotBeNull();
            result.Id.Should().Be(99);
            result.Username.Should().Be("TokenUser");
            result.Role.Should().Be("Admin");
        }

        [Fact]
        public void Verify_ShouldThrow_WhenTokenIsGarbage()
        {
            string badToken = "To.Nie.Jest.Token";
            Assert.Throws<SecurityTokenException>(() => _service.Verify(badToken));
        }
    }
}