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

namespace Market.Tests.Controllers
{
    public class FavoritesControllerTests
    {
        private readonly Mock<IFavoriteService> _mockService;
        private readonly FavoritesController _controller;

        public FavoritesControllerTests()
        {
            _mockService = new Mock<IFavoriteService>();
            _controller = new FavoritesController(_mockService.Object);
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
        public async Task ToggleFavorite_ShouldReturnOk_WhenSuccessful()
        {
            int userId = 10;
            int announcementId = 123;
            SetupUser(userId);

            var expectedResponse = new FavoriteResponseDto
            {
                IsFavorite = true,
                Message = "Dodano do ulubionych"
            };

            _mockService.Setup(s => s.ToggleFavoriteAsync(userId, announcementId))
                        .ReturnsAsync(expectedResponse);

            var result = await _controller.ToggleFavorite(announcementId);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResponse);

            _mockService.Verify(s => s.ToggleFavoriteAsync(userId, announcementId), Times.Once);
        }

        [Fact]
        public async Task ToggleFavorite_ShouldReturnNotFound_WhenAnnouncementDoesNotExist()
        {
            int userId = 10;
            int announcementId = 999;
            SetupUser(userId);

            _mockService.Setup(s => s.ToggleFavoriteAsync(userId, announcementId))
                        .ThrowsAsync(new KeyNotFoundException("Nie ma takiego ogłoszenia"));

            var result = await _controller.ToggleFavorite(announcementId);

            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("Nie ma takiego ogłoszenia");
        }

        [Fact]
        public async Task GetMyFavorites_ShouldReturnOk_WithList()
        {
            int userId = 5;
            SetupUser(userId);

            var favoritesList = new List<AnnouncementListDto>
            {
                new AnnouncementListDto { Id = 1, Title = "Fav1", Price = 100 },
                new AnnouncementListDto { Id = 2, Title = "Fav2", Price = 200 }
            };

            _mockService.Setup(s => s.GetUserFavoritesAsync(userId))
                        .ReturnsAsync(favoritesList);

            var result = await _controller.GetMyFavorites();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedList = okResult.Value.Should().BeAssignableTo<List<AnnouncementListDto>>().Subject;

            returnedList.Should().HaveCount(2);
            returnedList[0].Title.Should().Be("Fav1");
        }

        [Fact]
        public async Task GetFavoriteIds_ShouldReturnOk_WithListOfInts()
        {
            int userId = 5;
            SetupUser(userId);

            var idsList = new List<int> { 10, 20, 30 };

            _mockService.Setup(s => s.GetUserFavoriteIdsAsync(userId))
                        .ReturnsAsync(idsList);

            var result = await _controller.GetFavoriteIds();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedIds = okResult.Value.Should().BeAssignableTo<List<int>>().Subject;

            returnedIds.Should().HaveCount(3);
            returnedIds.Should().Contain(20);
        }
    }
}