using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Market.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class AnnouncementServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<ISearchService> _mockSearchService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<IAiModerationService> _mockAiModerationService;
        private readonly Mock<ILogService> _mockLogService;
        private readonly AnnouncementService _service;

        public AnnouncementServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _mockSearchService = new Mock<ISearchService>();
            _mockFileService = new Mock<IFileService>();
            _mockAiModerationService = new Mock<IAiModerationService>();
            _mockLogService = new Mock<ILogService>();

            _service = new AnnouncementService(
                _context,
                _mockSearchService.Object,
                _mockFileService.Object,
                _mockAiModerationService.Object,
                _mockLogService.Object
            );
        }

        private void SeedUser(int id, string username)
        {
            _context.Users.Add(new User
            {
                Id = id,
                Username = username,
                Email = "test@test.com",
                PasswordHash = "hash",
                Balance = 0
            });
            _context.SaveChanges();
        }

        private CreateAnnouncementDto CreateDto(string title, string category = "Inne")
        {
            return new CreateAnnouncementDto
            {
                Title = title,
                Description = "Opis testowy",
                Price = 100,
                Category = category,
                PhoneNumber = "123",
                ContactPreference = "Telefon",
                Location = "Warszawa",
                Latitude = "52.2",
                Longitude = "21.0",
                Photos = new List<IFormFile>(),
                VehicleDetails = category == "Pojazd" ? new VehicleDetailsDto
                {
                    Brand = "BMW",
                    Model = "X5",
                    Generation = "E70",
                    Year = 2010,
                    Mileage = 200000,
                    EnginePower = 300,
                    EngineCapacity = 3000,
                    FuelType = "Diesel",
                    Gearbox = "Automat",
                    BodyType = "SUV",
                    DriveType = "4x4",
                    Color = "Czarny",
                    VIN = "123",
                    State = "Używany"
                } : null
            };
        }


        [Fact]
        public async Task CreateAsync_ShouldSucceed_WhenAiApproves()
        {
            int userId = 1;
            SeedUser(userId, "Seller");
            var dto = CreateDto("Sprzedam Opla", "Pojazd");

            _mockAiModerationService
                .Setup(ai => ai.CheckContentAsync(dto.Title, dto.Description, dto.Price))
                .ReturnsAsync((true, "OK"));

            var id = await _service.CreateAsync(dto, userId);

            var saved = await _context.Announcements.Include(a => a.VehicleDetails).FirstOrDefaultAsync(a => a.Id == id);
            saved.Should().NotBeNull();
            saved.Title.Should().Be("Sprzedam Opla");
            saved.VehicleDetails.Should().NotBeNull();
            saved.VehicleDetails.Brand.Should().Be("BMW");
            _mockSearchService.Verify(s => s.IndexAnnouncementAsync(It.IsAny<Announcement>()), Times.Once);

            _mockLogService.Verify(l => l.LogAsync("NEW_ANNOUNCEMENT", It.IsAny<string>(), "Seller"), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenAiBlocks()
        {
            int userId = 1;
            SeedUser(userId, "Troll");
            var dto = CreateDto("Niedozwolona treść");

            _mockAiModerationService
                .Setup(ai => ai.CheckContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync((false, "Wulgaryzmy"));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(dto, userId));

            ex.Message.Should().Contain("zablokowane");

            (await _context.Announcements.CountAsync()).Should().Be(0);

            _mockLogService.Verify(l => l.LogAsync("AI_BLOCK", It.IsAny<string>(), "Troll"), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldSavePhotos_WhenProvided()
        {
            int userId = 1;
            SeedUser(userId, "FotoUser");
            var dto = CreateDto("Auto z fotką");

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns("photo.jpg");
            dto.Photos = new List<IFormFile> { fileMock.Object };

            _mockAiModerationService.Setup(x => x.CheckContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync((true, "OK"));

            _mockFileService.Setup(f => f.SaveFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("uploads/unique_photo.jpg");

            var id = await _service.CreateAsync(dto, userId);

            var saved = await _context.Announcements.Include(a => a.Photos).FirstOrDefaultAsync(a => a.Id == id);
            saved.Photos.Should().HaveCount(1);
            saved.PhotoUrl.Should().Be("uploads/unique_photo.jpg"); saved.Photos.First().IsMain.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ShouldSucceed_WhenUserIsOwner()
        {
            int userId = 10;
            var announcement = new Announcement
            {
                Id = 100,
                UserId = userId,
                Title = "Do kasacji",
                PhotoUrl = "img.jpg",
                Photos = new List<AnnouncementPhoto> { new AnnouncementPhoto { PhotoUrl = "img.jpg" } }
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(100, userId);

            var deleted = await _context.Announcements.FindAsync(100);
            deleted.Should().BeNull();

            _mockFileService.Verify(f => f.DeleteFile("img.jpg"), Times.Once);

            _mockSearchService.Verify(s => s.RemoveAsync("100"), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenUserIsNotOwner()
        {
            int ownerId = 1;
            int hackerId = 2;
            var announcement = new Announcement { Id = 1, UserId = ownerId, Title = "Nie twoje" };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DeleteAsync(1, hackerId));
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateFields_AndReindex()
        {
            int userId = 1;
            var announcement = new Announcement
            {
                Id = 1,
                UserId = userId,
                Title = "Stary Tytuł",
                Price = 100,
                Category = "Inne"
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var updateDto = CreateDto("Nowy Tytuł");
            updateDto.Price = 200;

            _mockAiModerationService.Setup(x => x.CheckContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync((true, "OK"));

            await _service.UpdateAsync(1, updateDto, userId);

            var updated = await _context.Announcements.FindAsync(1);
            updated.Title.Should().Be("Nowy Tytuł");
            updated.Price.Should().Be(200);

            _mockSearchService.Verify(s => s.IndexAnnouncementAsync(It.IsAny<Announcement>()), Times.Once);
        }

        [Fact]
        public async Task RenewAsync_ShouldExtendExpirationDate()
        {
            int userId = 1;
            var oldDate = DateTime.UtcNow.AddDays(-1); var announcement = new Announcement
            {
                Id = 1,
                UserId = userId,
                IsActive = false,
                ExpiresAt = oldDate
            };
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            await _service.RenewAsync(1, userId);

            var renewed = await _context.Announcements.FindAsync(1);
            renewed.IsActive.Should().BeTrue();
            renewed.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(29));
        }
    }
}
