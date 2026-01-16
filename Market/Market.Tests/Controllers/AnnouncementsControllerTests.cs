using FluentAssertions;
using Market.Controllers;
using Market.DTOs;
using Market.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System;

namespace Market.Tests.Controllers
{
    public class AnnouncementsControllerTests
    {
        private readonly Mock<IAnnouncementService> _mockService;
        private readonly AnnouncementsController _controller;

        public AnnouncementsControllerTests()
        {
            _mockService = new Mock<IAnnouncementService>();

            _controller = new AnnouncementsController(_mockService.Object);
        }

        private void SetupUser(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),                
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }


        [Fact]
        public async Task CreateAnnouncement_ShouldReturnCreated_WhenDataIsValid()
        {
            int userId = 10;
            int newAnnouncementId = 55;
            SetupUser(userId);
            var dto = new CreateAnnouncementDto { Title = "Sprzedam Opla", Price = 1000 };

            _mockService.Setup(s => s.CreateAsync(dto, userId))
                        .ReturnsAsync(newAnnouncementId);

            var result = await _controller.CreateAnnouncement(dto);

            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.StatusCode.Should().Be(201);
            createdResult.ActionName.Should().Be(nameof(AnnouncementsController.GetAnnouncement));
            createdResult.RouteValues["id"].Should().Be(newAnnouncementId);
        }

        [Fact]
        public async Task CreateAnnouncement_ShouldThrow_WhenPriceIsZeroOrLess()
        {
            var dto = new CreateAnnouncementDto { Title = "Darmowe", Price = 0 };
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _controller.CreateAnnouncement(dto));
        }


        [Fact]
        public async Task GetAnnouncement_ShouldReturnOk_WhenExists()
        {
            int id = 1;
            var dto = new AnnouncementDto { Id = id, Title = "Test" };

            _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetAnnouncement(id);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnDto = okResult.Value.Should().BeOfType<AnnouncementDto>().Subject;
            returnDto.Title.Should().Be("Test");
        }

        [Fact]
        public async Task GetAnnouncement_ShouldReturnNotFound_WhenNull()
        {
            _mockService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((AnnouncementDto?)null);

            var result = await _controller.GetAnnouncement(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetUserAnnouncements_ShouldReturnOk_WithList()
        {
            int userId = 5;
            SetupUser(userId);

            var list = new List<AnnouncementListDto>
            {
                new AnnouncementListDto { Title = "A1" },
                new AnnouncementListDto { Title = "A2" }
            };

            _mockService.Setup(s => s.GetUserAnnouncementsAsync(userId))
                        .ReturnsAsync(list);

            var result = await _controller.GetUserAnnouncements();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnList = okResult.Value.Should().BeAssignableTo<List<AnnouncementListDto>>().Subject;
            returnList.Should().HaveCount(2);
        }


        [Fact]
        public async Task Renew_ShouldReturnOk()
        {
            int userId = 1;
            int announcementId = 100;
            SetupUser(userId);

            _mockService.Setup(s => s.RenewAsync(announcementId, userId)).Returns(Task.CompletedTask);

            var result = await _controller.Renew(announcementId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent()
        {
            int userId = 1;
            int announcementId = 100;
            SetupUser(userId);

            _mockService.Setup(s => s.DeleteAsync(announcementId, userId)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(announcementId);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_ShouldReturnOk()
        {
            int userId = 1;
            int announcementId = 100;
            SetupUser(userId);
            var dto = new CreateAnnouncementDto { Title = "Update" };

            _mockService.Setup(s => s.UpdateAsync(announcementId, dto, userId)).Returns(Task.CompletedTask);

            var result = await _controller.Update(announcementId, dto);

            result.Should().BeOfType<OkObjectResult>();
        }


        [Fact]
        public async Task Search_ShouldReturnOk_WithResults()
        {
            var query = new SearchQueryDto { Query = "Auto" };
            var searchResult = new SearchResultDto { TotalHits = 5 };

            _mockService.Setup(s => s.SearchAsync(query)).ReturnsAsync(searchResult);

            var result = await _controller.Search(query);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(searchResult);
        }

        [Fact]
        public async Task SyncAll_ShouldReturnOk_WithCount()
        {
            _mockService.Setup(s => s.SyncAllToSearchAsync()).ReturnsAsync(100);

            var result = await _controller.SyncAll();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }
    }
}