using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.DTOs.Admin;
using Market.Helpers;
using Market.Interfaces;
using Market.Models;
using Market.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class AdminServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogService> _mockLogService;
        private readonly Mock<ISearchService> _mockSearchService;
        private readonly AdminService _service;




        public AdminServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;


            _context = new AppDbContext(options);

            _mockFileService = new Mock<IFileService>();
            _mockLogService = new Mock<ILogService>();
            _mockSearchService = new Mock<ISearchService>();

            _service = new AdminService(
                _context,
                _mockFileService.Object,
                _mockLogService.Object,
                _mockSearchService.Object
            );
        }


        private User CreateUser(int id, string role, string name)
        {
            return new User
            {
                Id = id,
                Role = role,
                Username = name,
                Email = $"{name.ToLower()}@test.com",
                PasswordHash = "SecretHash123!",
                Balance = 0,
                PhoneNumber = "123456789",
                IsBanned = false,
                Announcements = new List<Announcement>()
            };
        }

        private Announcement CreateAnnouncement(int id, int userId, string title)
        {
            return new Announcement
            {
                Id = id,
                UserId = userId,
                Title = title,
                Description = "Przykładowy opis testowy.",
                Price = 100m,
                CreatedAt = DateTime.Now,
                IsActive = true,

                Category = "Pojazdy",

                Location = "Warszawa",
                PhoneNumber = "123456789",
                ContactPreference = "Telefon",
                PhotoUrl = "uploads/default.jpg",

                Features = new List<AnnouncementFeature>(),
                Photos = new List<AnnouncementPhoto>(),

                VehicleDetails = new VehicleDetails
                {
                    AnnouncementId = id,
                    Brand = "TestBrand",
                    Model = "TestModel",
                    Generation = "Gen1",
                    Year = 2020,
                    Mileage = 100000,
                    EngineCapacity = 2000,
                    EnginePower = 150,
                    FuelType = "Benzyna",
                    Gearbox = "Manualna",
                    BodyType = "Sedan",
                    DriveType = "FWD",
                    Color = "Czarny",
                    VIN = "ABC1234567890",
                    State = "Używany"
                },
                PartDetails = null
            };
        }


        [Fact]
        public async Task GetStatsAsync_ShouldReturnCorrectCounts()
        {
            _context.Users.Add(CreateUser(1, "User", "User1"));
            _context.Users.Add(CreateUser(2, "User", "User2"));

            var a1 = CreateAnnouncement(1, 1, "A1");
            a1.CreatedAt = DateTime.Today;
            var a2 = CreateAnnouncement(2, 2, "A2");
            a2.CreatedAt = DateTime.Today.AddDays(-5);
            _context.Announcements.AddRange(a1, a2);
            await _context.SaveChangesAsync();

            var stats = await _service.GetStatsAsync();

            stats.TotalUsers.Should().Be(2);
            stats.TotalAnnouncements.Should().Be(2);
            stats.NewAnnouncementsToday.Should().Be(1);
        }

        [Fact]
        public async Task ToggleBanAsync_ShouldBanUser_WhenNotBanned()
        {
            int userId = 1;
            var user = CreateUser(userId, "User", "Troll");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var isBanned = await _service.ToggleBanAsync(userId, "Admin1");

            isBanned.Should().BeTrue();
            var dbUser = await _context.Users.FindAsync(userId);
            dbUser.IsBanned.Should().BeTrue();

            _mockLogService.Verify(l => l.LogAsync("USER_BAN", It.IsAny<string>(), "Admin1"), Times.Once);
        }

        [Fact]
        public async Task ToggleBanAsync_ShouldThrow_WhenTryingToBanAdmin()
        {
            int adminId = 99;
            var admin = CreateUser(adminId, "Admin", "Boss");
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ToggleBanAsync(adminId, "Admin1"));

            ex.Message.Should().Contain("Nie można zbanować administratora");
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldRemoveUser_AndFiles_AndSearchIndex()
        {
            int userId = 10;
            var user = CreateUser(userId, "User", "DoKasacji");

            var announcement = CreateAnnouncement(100, userId, "Grat");
            announcement.PhotoUrl = "uploads/foto.jpg";

            user.Announcements.Add(announcement);

            _context.Users.Add(user);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await _service.DeleteUserAsync(userId, "SuperAdmin");

            var deletedUser = await _context.Users.FindAsync(userId);
            deletedUser.Should().BeNull();

            var deletedAnnouncement = await _context.Announcements.FindAsync(100);
            deletedAnnouncement.Should().BeNull();

            _mockFileService.Verify(f => f.DeleteFile("uploads/foto.jpg"), Times.Once);

            _mockSearchService.Verify(s => s.RemoveAsync("100"), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldSucceed_EvenIfAlgoliaFails()
        {
            int userId = 5;
            var user = CreateUser(userId, "User", "Pechowiec");

            var announcement = CreateAnnouncement(50, userId, "Auto");
            user.Announcements.Add(announcement);

            _context.Users.Add(user);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            _mockSearchService.Setup(s => s.RemoveAsync(It.IsAny<string>()))
                              .ThrowsAsync(new Exception("Algolia down"));

            await _service.DeleteUserAsync(userId, "Admin");

            var dbUser = await _context.Users.FindAsync(userId);
            dbUser.Should().BeNull();
        }

        [Fact]
        public async Task GetUsersAsync_ShouldFilterBySearchTerm()
        {
            var u1 = CreateUser(1, "User", "Janusz");
            u1.Email = "janusz@wp.pl";

            var u2 = CreateUser(2, "User", "Grazyna");
            u2.Email = "grazyna@wp.pl";

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var pagination = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "wp.pl"
            };

            var result = await _service.GetUsersAsync(pagination);

            result.Items.Should().HaveCount(2); result.Items.Should().Contain(u => u.Username == "Janusz");
            result.Items.Should().Contain(u => u.Username == "Grazyna");
        }
    }
}