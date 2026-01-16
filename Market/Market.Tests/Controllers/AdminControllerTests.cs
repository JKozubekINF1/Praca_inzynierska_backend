using FluentAssertions;
using Market.Controllers;
using Market.DTOs;
using Market.DTOs.Admin;
using Market.Helpers;
using Market.Interfaces;
using Market.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Market.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IAdminService> _mockService;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockService = new Mock<IAdminService>();
            _controller = new AdminController(_mockService.Object);
        }

        private void SetupAdminUser(string adminName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(ClaimTypes.Name, adminName),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }


        [Fact]
        public async Task GetAllUsers_ShouldReturnOk_WithPagedResult()
        {
            var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var expectedResult = new PagedResult<AdminUserDto>(new List<AdminUserDto>(), 0, 1, 10);

            _mockService.Setup(s => s.GetUsersAsync(pagination))
                        .ReturnsAsync(expectedResult);

            var result = await _controller.GetAllUsers(pagination);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResult);
        }

        [Fact]
        public async Task DeleteUser_ShouldCallService_WithCorrectAdminName()
        {
            int userIdToDelete = 5;
            string adminName = "SuperAdmin";
            SetupAdminUser(adminName);
            _mockService.Setup(s => s.DeleteUserAsync(userIdToDelete, adminName))
                        .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser(userIdToDelete);

            result.Should().BeOfType<OkObjectResult>();

            _mockService.Verify(s => s.DeleteUserAsync(userIdToDelete, adminName), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ShouldReturnOk_WithDetails()
        {
            int userId = 1;
            var userDetail = new { Id = 1, Username = "Test" };
            _mockService.Setup(s => s.GetUserDetailAsync(userId)).ReturnsAsync(userDetail);

            var result = await _controller.GetUserById(userId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(userDetail);
        }

        [Fact]
        public async Task ToggleBan_ShouldReturnOk_AndCorrectMessage_WhenBanned()
        {
            int userId = 1;
            string adminName = "Mod1";
            SetupAdminUser(adminName);

            _mockService.Setup(s => s.ToggleBanAsync(userId, adminName))
                        .ReturnsAsync(true);

            var result = await _controller.ToggleBan(userId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = okResult.Value;


            var type = value.GetType();

            var message = (string)type.GetProperty("Message").GetValue(value);
            var isBannedResult = (bool)type.GetProperty("IsBanned").GetValue(value);

            message.Should().Be("Użytkownik zbanowany.");
            isBannedResult.Should().BeTrue();

            _mockService.Verify(s => s.ToggleBanAsync(userId, adminName), Times.Once);
        }


        [Fact]
        public async Task GetAllAnnouncements_ShouldReturnOk()
        {
            var pagination = new PaginationParams();
            var expectedResult = new PagedResult<AdminAnnouncementDto>(new List<AdminAnnouncementDto>(), 0, 1, 10);

            _mockService.Setup(s => s.GetAnnouncementsAsync(pagination)).ReturnsAsync(expectedResult);

            var result = await _controller.GetAllAnnouncements(pagination);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteAnnouncement_ShouldCallService_WithAdminName()
        {
            int id = 100;
            string adminName = "AdminX";
            SetupAdminUser(adminName);

            _mockService.Setup(s => s.DeleteAnnouncementAsync(id, adminName))
                        .Returns(Task.CompletedTask);

            var result = await _controller.DeleteAnnouncement(id);

            result.Should().BeOfType<OkObjectResult>();
            _mockService.Verify(s => s.DeleteAnnouncementAsync(id, adminName), Times.Once);
        }


        [Fact]
        public async Task GetLogs_ShouldReturnOk_WithList()
        {
            var logs = new List<SystemLog> { new SystemLog { Message = "Log1" } };
            _mockService.Setup(s => s.GetLogsAsync()).ReturnsAsync(logs);

            var result = await _controller.GetLogs();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedLogs = okResult.Value.Should().BeAssignableTo<List<SystemLog>>().Subject;
            returnedLogs.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetStats_ShouldReturnOk()
        {
            var stats = new AdminStatsDto { TotalUsers = 100 };
            _mockService.Setup(s => s.GetStatsAsync()).ReturnsAsync(stats);

            var result = await _controller.GetStats();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(stats);
        }
    }
}