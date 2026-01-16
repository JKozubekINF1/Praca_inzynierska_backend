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
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IAnnouncementService> _mockAnnouncementService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockAnnouncementService = new Mock<IAnnouncementService>();

            _controller = new UsersController(_mockUserService.Object, _mockAnnouncementService.Object);
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
        public async Task GetProfile_ShouldReturnOk_WithUserData()
        {
            int userId = 1;
            SetupUser(userId);

            var userDto = new UserDto
            {
                Username = "Janusz",
                Email = "janusz@test.com",
                Name = "Jan",
                Surname = "Kowalski",
                PhoneNumber = "123456789"
            };

            _mockUserService.Setup(s => s.GetProfileAsync(userId))
                            .ReturnsAsync(userDto);

            var result = await _controller.GetProfile();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(userDto);
        }

        [Fact]
        public async Task UpdateProfile_ShouldReturnOk_WhenSuccess()
        {
            int userId = 1;
            SetupUser(userId);

            var dto = new UserDto
            {
                Username = "NewName",
                Email = "new@test.com"
            };

            _mockUserService.Setup(s => s.UpdateProfileAsync(userId, dto))
                            .Returns(Task.CompletedTask);

            var result = await _controller.UpdateProfile(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Profil zaktualizowany pomyślnie." });

            _mockUserService.Verify(s => s.UpdateProfileAsync(userId, dto), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_ShouldReturnOk_WhenSuccess()
        {
            int userId = 1;
            SetupUser(userId);

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword1!",
                NewPassword = "NewPassword1!"
            };

            _mockUserService.Setup(s => s.ChangePasswordAsync(userId, dto))
                            .Returns(Task.CompletedTask);

            var result = await _controller.ChangePassword(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Hasło zmienione pomyślnie." });
        }

        [Fact]
        public async Task GetUserAnnouncements_ShouldReturnOk_WithList()
        {
            int userId = 5;
            SetupUser(userId);

            var announcements = new List<AnnouncementListDto>
            {
                new AnnouncementListDto { Id = 1, Title = "Moje ogłoszenie 1", Price = 100 },
                new AnnouncementListDto { Id = 2, Title = "Moje ogłoszenie 2", Price = 200 }
            };

            _mockAnnouncementService.Setup(s => s.GetUserAnnouncementsAsync(userId))
                                    .ReturnsAsync(announcements);

            var result = await _controller.GetUserAnnouncements();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedList = okResult.Value.Should().BeAssignableTo<List<AnnouncementListDto>>().Subject;

            returnedList.Should().HaveCount(2);
            returnedList[0].Title.Should().Be("Moje ogłoszenie 1");
        }
    }
}