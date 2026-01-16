using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Market.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class FavoriteServiceTests
    {
        private readonly AppDbContext _context;
        private readonly FavoriteService _service;

        public FavoriteServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _service = new FavoriteService(_context);
        }


        private User CreateUser(int id)
        {
            return new User
            {
                Id = id,
                Username = $"User{id}",
                Email = $"user{id}@test.com",
                PasswordHash = "hash",
                Role = "User",
                IsBanned = false,
                PhoneNumber = "123"
            };
        }

        private Announcement CreateAnnouncement(int id)
        {
            return new Announcement
            {
                Id = id,
                Title = $"Ogłoszenie {id}",
                Description = "Opis",
                Price = 100,
                Category = "Pojazd",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserId = 1,
                Location = "Warszawa",
                PhoneNumber = "123",
                ContactPreference = "Chat",
                Photos = new List<AnnouncementPhoto>(),
                VehicleDetails = new VehicleDetails()
            };
        }


        [Fact]
        public async Task ToggleFavoriteAsync_ShouldAddFavorite_WhenItDoesNotExist()
        {
            int userId = 10;
            int announcementId = 100;

            _context.Users.Add(CreateUser(userId));
            _context.Announcements.Add(CreateAnnouncement(announcementId));
            await _context.SaveChangesAsync();

            var result = await _service.ToggleFavoriteAsync(userId, announcementId);

            result.IsFavorite.Should().BeTrue();
            result.Message.Should().Contain("Dodano");

            var favInDb = await _context.Favorites.FirstOrDefaultAsync();
            favInDb.Should().NotBeNull();
            favInDb.UserId.Should().Be(userId);
            favInDb.AnnouncementId.Should().Be(announcementId);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldRemoveFavorite_WhenItExists()
        {
            int userId = 10;
            int announcementId = 100;

            _context.Users.Add(CreateUser(userId));
            _context.Announcements.Add(CreateAnnouncement(announcementId));

            _context.Favorites.Add(new Favorite
            {
                UserId = userId,
                AnnouncementId = announcementId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _service.ToggleFavoriteAsync(userId, announcementId);

            result.IsFavorite.Should().BeFalse();
            result.Message.Should().Contain("Usunięto");

            var count = await _context.Favorites.CountAsync();
            count.Should().Be(0);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldThrow_WhenAnnouncementNotFound()
        {

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ToggleFavoriteAsync(1, 999));
        }

        [Fact]
        public async Task GetUserFavoritesAsync_ShouldReturnListOfAnnouncements()
        {
            int userId = 1;

            var a1 = CreateAnnouncement(10);
            a1.Title = "Ulubione Auto";

            var a2 = CreateAnnouncement(20);
            a2.Title = "Ulubiony Rower";

            _context.Announcements.AddRange(a1, a2);

            _context.Favorites.Add(new Favorite { UserId = userId, AnnouncementId = 10 });
            _context.Favorites.Add(new Favorite { UserId = userId, AnnouncementId = 20 });

            _context.Favorites.Add(new Favorite { UserId = 99, AnnouncementId = 10 });

            await _context.SaveChangesAsync();

            var result = await _service.GetUserFavoritesAsync(userId);

            result.Should().HaveCount(2);
            result.Should().Contain(x => x.Title == "Ulubione Auto");
            result.Should().Contain(x => x.Title == "Ulubiony Rower");
        }

        [Fact]
        public async Task GetUserFavoriteIdsAsync_ShouldReturnListOfInts()
        {
            int userId = 5;
            _context.Favorites.Add(new Favorite { UserId = userId, AnnouncementId = 100 });
            _context.Favorites.Add(new Favorite { UserId = userId, AnnouncementId = 200 });
            _context.Favorites.Add(new Favorite { UserId = userId, AnnouncementId = 300 });
            await _context.SaveChangesAsync();

            var result = await _service.GetUserFavoriteIdsAsync(userId);

            result.Should().HaveCount(3);
            result.Should().Contain(new[] { 100, 200, 300 });
        }
    }
}