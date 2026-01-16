using FluentAssertions;
using Market.Controllers;
using Market.DTOs;
using Market.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Market.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockService;
        private readonly Mock<IResponseCookies> _mockResponseCookies;
        private readonly Mock<IRequestCookieCollection> _mockRequestCookies;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockService = new Mock<IAuthService>();

            _mockResponseCookies = new Mock<IResponseCookies>();
            _mockRequestCookies = new Mock<IRequestCookieCollection>();

            _controller = new AuthController(_mockService.Object);
        }


        private void SetupResponseContext()
        {
            var mockResponse = new Mock<HttpResponse>();
            mockResponse.Setup(r => r.Cookies).Returns(_mockResponseCookies.Object);

            var mockContext = new Mock<HttpContext>();
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockContext.Object
            };
        }

        private void SetupRequestContext(string? tokenValue)
        {
            var mockRequest = new Mock<HttpRequest>();
            _mockRequestCookies.Setup(c => c["AuthToken"]).Returns(tokenValue);
            mockRequest.Setup(r => r.Cookies).Returns(_mockRequestCookies.Object);

            var mockContext = new Mock<HttpContext>();
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = mockContext.Object
            };
        }


        [Fact]
        public async Task Register_ShouldReturnOk_WhenSuccess()
        {
            var dto = new RegisterDto
            {
                Username = "Test",
                Email = "test@test.com",
                Password = "Password1!",
                RecaptchaToken = "token"
            };

            _mockService.Setup(s => s.RegisterAsync(dto))
                        .ReturnsAsync((true, "Sukces"));

            var result = await _controller.Register(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Sukces" });
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenFail()
        {
            var dto = new RegisterDto
            {
                Username = "Test",
                Email = "test@test.com",
                Password = "Password1!",
                RecaptchaToken = "token"
            };

            _mockService.Setup(s => s.RegisterAsync(dto))
                        .ReturnsAsync((false, "Błąd walidacji"));

            var result = await _controller.Register(dto);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeEquivalentTo(new { Message = "Błąd walidacji" });
        }


        [Fact]
        public async Task Login_ShouldReturnOk_AndSetCookie_WhenSuccess()
        {
            SetupResponseContext();

            var dto = new LoginDto
            {
                Username = "User",
                Password = "Password1!",
                RecaptchaToken = "token"
            };

            string fakeToken = "abc.def.ghi";

            _mockService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(fakeToken);

            var result = await _controller.Login(dto);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { Message = "Zalogowano pomyślnie." });

            _mockResponseCookies.Verify(c => c.Append(
                "AuthToken",
                fakeToken,
                It.Is<CookieOptions>(opt => opt.HttpOnly == true && opt.Secure == true)
            ), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenServiceReturnsNull()
        {
            var dto = new LoginDto
            {
                Username = "User",
                Password = "WrongPassword",
                RecaptchaToken = "token"
            };

            _mockService.Setup(s => s.LoginAsync(dto)).ReturnsAsync((string?)null);

            var result = await _controller.Login(dto);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Logout_ShouldDeleteCookie()
        {
            SetupResponseContext();

            var result = _controller.Logout();

            result.Should().BeOfType<OkObjectResult>();
            _mockResponseCookies.Verify(c => c.Delete("AuthToken"), Times.Once);
        }


        [Fact]
        public void Verify_ShouldReturnOk_WhenCookieIsValid()
        {
            string token = "valid_token";
            SetupRequestContext(token);

            var userDto = new VerifyResultDto
            {
                Id = 1,
                Username = "TestUser",
                Role = "Admin"
            };

            _mockService.Setup(s => s.Verify(token)).Returns(userDto);

            var result = _controller.Verify();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            okResult.Value.Should().BeEquivalentTo(new
            {
                Message = "Token ważny",
                User = userDto
            });
        }

        [Fact]
        public void Verify_ShouldReturnUnauthorized_WhenCookieIsMissing()
        {
            SetupRequestContext(null);

            var result = _controller.Verify();

            var unauth = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauth.Value.Should().BeEquivalentTo(new { Message = "Brak tokenu." });
        }

        [Fact]
        public void Verify_ShouldReturnUnauthorized_WhenTokenIsInvalid()
        {
            string badToken = "bad_token";
            SetupRequestContext(badToken);

            _mockService.Setup(s => s.Verify(badToken)).Throws(new Exception("Invalid token"));

            var result = _controller.Verify();

            var unauth = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauth.Value.Should().BeEquivalentTo(new { Message = "Nieprawidłowy lub wygasły token." });
        }
    }
}