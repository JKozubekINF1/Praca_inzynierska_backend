using FluentAssertions;
using Market.Data;
using Market.DTOs;
using Market.Interfaces;
using Market.Models;
using Market.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

using Xunit;
using System;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<ISearchService> _mockSearchService;
        private readonly Mock<ILogService> _mockLogService;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            _mockSearchService = new Mock<ISearchService>();
            _mockLogService = new Mock<ILogService>();

            _service = new OrderService(_context, _mockSearchService.Object, _mockLogService.Object);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldSucceed_WhenFundsAreSufficient()
        {
            int buyerId = 10;
            int sellerId = 20;
            int announcementId = 100;
            decimal price = 500m;

            var buyer = new User { Id = buyerId, Balance = 1000m, Username = "Kupujacy", Email = "k@k.pl", PasswordHash = "hash", Role = "User" };
            var seller = new User { Id = sellerId, Balance = 0m, Username = "Sprzedawca", Email = "s@s.pl", PasswordHash = "hash", Role = "User" };

            var announcement = new Announcement
            {
                Id = announcementId,
                UserId = sellerId,
                User = seller,
                Title = "Rower",
                Price = price,
                IsActive = true
            };

            _context.Users.AddRange(buyer, seller);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var dto = new CreateOrderDto
            {
                AnnouncementId = announcementId,
                DeliveryMethod = "Paczkomat",
                DeliveryAddress = "WAW01",
                DeliveryPointName = "Paczkomat Centrum"
            };

            var result = await _service.CreateOrderAsync(buyerId, dto);


            result.Should().NotBeNull();
            result.AmountPaid.Should().Be(price);
            result.RemainingBalance.Should().Be(500m);
            var dbBuyer = await _context.Users.FindAsync(buyerId);
            dbBuyer.Balance.Should().Be(500m);

            var dbSeller = await _context.Users.FindAsync(sellerId);
            dbSeller.Balance.Should().Be(500m);

            var dbAnnouncement = await _context.Announcements.FindAsync(announcementId);
            dbAnnouncement.IsActive.Should().BeFalse();

            var order = await _context.Orders.FirstOrDefaultAsync();
            order.Should().NotBeNull();
            order.BuyerId.Should().Be(buyerId);
            order.Status.Should().Be(OrderStatus.Paid);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrow_WhenInsufficientFunds()
        {
            int buyerId = 1;
            var buyer = new User { Id = buyerId, Balance = 10m, Username = "Biedny", Email = "b@b.pl", PasswordHash = "x", Role = "User" };
            var seller = new User { Id = 2, Username = "Sprzedawca", Email = "s@s.pl", PasswordHash = "x", Role = "User" };

            var announcement = new Announcement { Id = 1, UserId = 2, User = seller, Price = 100m, IsActive = true, Title = "Drogo" };

            _context.Users.AddRange(buyer, seller);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var dto = new CreateOrderDto { AnnouncementId = 1 };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(buyerId, dto));

            ex.Message.Should().Contain("Niewystarczające środki");
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrow_WhenBuyingOwnItem()
        {
            int userId = 1;
            var user = new User { Id = userId, Balance = 1000m, Username = "Ja", Email = "ja@ja.pl", PasswordHash = "x", Role = "User" };
            var announcement = new Announcement { Id = 1, UserId = userId, User = user, IsActive = true, Title = "Moje", Price = 10m };

            _context.Users.Add(user);
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            var dto = new CreateOrderDto { AnnouncementId = 1 };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateOrderAsync(userId, dto));

            ex.Message.Should().Contain("własnego przedmiotu");
        }
    }
}