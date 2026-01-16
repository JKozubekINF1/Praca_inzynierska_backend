using FluentAssertions;
using Market.Controllers;
using Market.DTOs;
using Market.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Market.Tests.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<IOrderService> _mockService;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockService = new Mock<IOrderService>();
            _controller = new TransactionController(_mockService.Object);
        }

        private void SetupUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }


        [Fact]
        public async Task GetBalance_ShouldReturnOk_WithAmount()
        {
            int userId = 1;
            decimal fakeBalance = 150.50m;
            SetupUser(userId);

            _mockService.Setup(s => s.GetBalanceAsync(userId))
                        .ReturnsAsync(fakeBalance);

            var result = await _controller.GetBalance();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { balance = fakeBalance });
        }


        [Fact]
        public async Task TopUp_ShouldReturnOk_WhenSuccess()
        {
            int userId = 1;
            decimal amount = 50m;
            decimal newBalance = 200m;

            SetupUser(userId);

            var dto = new TopUpDto { Amount = amount };

            _mockService.Setup(s => s.TopUpAsync(userId, amount))
                        .ReturnsAsync(newBalance);

            var result = await _controller.TopUp(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { newBalance = newBalance });
        }

        [Fact]
        public async Task TopUp_ShouldReturnBadRequest_WhenAmountIsInvalid()
        {
            int userId = 1;
            SetupUser(userId);
            var dto = new TopUpDto { Amount = -100 };

            _mockService.Setup(s => s.TopUpAsync(userId, -100))
                        .ThrowsAsync(new ArgumentException("Kwota musi być dodatnia"));

            var result = await _controller.TopUp(dto);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { message = "Kwota musi być dodatnia" });
        }


        [Fact]
        public async Task BuyItem_ShouldReturnOk_WhenSuccess()
        {
            int userId = 1;
            SetupUser(userId);

            var dto = new CreateOrderDto
            {
                AnnouncementId = 10,
                DeliveryMethod = "Kurier",
                DeliveryAddress = "Ulica 1"
            };

            var orderResult = new OrderResultDto
            {
                OrderId = 555,
                Message = "Kupiono pomyślnie"
            };

            _mockService.Setup(s => s.CreateOrderAsync(userId, dto))
                        .ReturnsAsync(orderResult);

            var result = await _controller.BuyItem(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(orderResult);
        }

        [Fact]
        public async Task BuyItem_ShouldReturnNotFound_WhenAnnouncementMissing()
        {
            int userId = 1;
            SetupUser(userId);
            var dto = new CreateOrderDto
            {
                AnnouncementId = 999,
                DeliveryMethod = "X",
                DeliveryAddress = "Y"
            };

            _mockService.Setup(s => s.CreateOrderAsync(userId, dto))
                        .ThrowsAsync(new KeyNotFoundException("Brak ogłoszenia"));

            var result = await _controller.BuyItem(dto);

            var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeEquivalentTo(new { message = "Brak ogłoszenia" });
        }

        [Fact]
        public async Task BuyItem_ShouldReturnBadRequest_WhenInsufficientFunds()
        {
            int userId = 1;
            SetupUser(userId);
            var dto = new CreateOrderDto
            {
                AnnouncementId = 10,
                DeliveryMethod = "X",
                DeliveryAddress = "Y"
            };

            _mockService.Setup(s => s.CreateOrderAsync(userId, dto))
                        .ThrowsAsync(new InvalidOperationException("Brak środków"));

            var result = await _controller.BuyItem(dto);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { message = "Brak środków" });
        }


        [Fact]
        public async Task GetHistory_ShouldReturnList()
        {
            int userId = 5;
            SetupUser(userId);

            var historyList = new List<OrderHistoryDto>
            {
                new OrderHistoryDto
                {
                    OrderId = 1,
                    AnnouncementTitle = "Auto",
                    TotalAmount = 1000m,
                    Status = "Completed",
                    DeliveryMethod = "Personal",
                    OrderDate = DateTime.Now
                },
                new OrderHistoryDto
                {
                    OrderId = 2,
                    AnnouncementTitle = "Rower",
                    TotalAmount = 500m,
                    Status = "Paid",
                    DeliveryMethod = "Courier",
                    OrderDate = DateTime.Now.AddDays(-1)
                }
            };

            _mockService.Setup(s => s.GetUserOrdersAsync(userId))
                        .ReturnsAsync(historyList);

            var result = await _controller.GetHistory();

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedList = okResult.Value.Should().BeAssignableTo<IEnumerable<OrderHistoryDto>>().Subject;

            returnedList.Should().HaveCount(2);
        }
    }
}