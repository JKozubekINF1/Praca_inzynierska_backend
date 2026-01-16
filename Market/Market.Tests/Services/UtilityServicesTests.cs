using FluentAssertions;
using Market.Data;
using Market.Models;
using Market.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Market.Tests.Services
{
    public class UtilityServicesTests
    {
        [Fact]
        public async Task LogService_ShouldSaveLogToDatabase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            var service = new LogService(context);

            await service.LogAsync("TEST_ACTION", "Test message", "User1");

            var log = await context.SystemLogs.FirstOrDefaultAsync();
            log.Should().NotBeNull();
            log.Action.Should().Be("TEST_ACTION");
            log.Message.Should().Be("Test message");
            log.Username.Should().Be("User1");
        }

        [Fact]
        public async Task LocalFileService_ShouldSaveFileToDisk()
        {
            var mockEnv = new Mock<IWebHostEnvironment>();
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            mockEnv.Setup(e => e.WebRootPath).Returns(tempPath);

            var service = new LocalFileService(mockEnv.Object);

            var mockFile = new Mock<IFormFile>();
            var content = "Hello World";
            var fileName = "test.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);

            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    ms.Position = 0;
                    ms.CopyTo(stream);
                })
                .Returns(Task.CompletedTask);

            var savedPath = await service.SaveFileAsync(mockFile.Object);

            savedPath.Should().StartWith("/uploads/");
            savedPath.Should().EndWith(".txt");

            var fullPath = Path.Combine(tempPath, savedPath.TrimStart('/'));
            File.Exists(fullPath).Should().BeTrue();

            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
        }

        [Fact]
        public async Task OpenAiModerationService_ShouldReturnSafe_WhenAiSaysSo()
        {
            var mockHandler = new Mock<HttpMessageHandler>();

            var jsonResponse = "{\"choices\": [{\"message\": {\"content\": \"{\\\"isSafe\\\": true, \\\"reason\\\": \\\"OK\\\"}\"}}]}";

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(mockHandler.Object);

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAi:ApiKey"]).Returns("dummy_key");

            var service = new OpenAiModerationService(httpClient, mockConfig.Object);

            var result = await service.CheckContentAsync("Auto", "Sprzedam auto", 100);

            result.IsSafe.Should().BeTrue();
            result.Reason.Should().Be("OK");
        }

        [Fact]
        public async Task OpenAiModerationService_ShouldReturnUnsafe_WhenAiFlagsContent()
        {
            var mockHandler = new Mock<HttpMessageHandler>();

            var jsonResponse = "{\"choices\": [{\"message\": {\"content\": \"{\\\"isSafe\\\": false, \\\"reason\\\": \\\"Scam detected\\\"}\"}}]}";

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAi:ApiKey"]).Returns("dummy_key");

            var service = new OpenAiModerationService(httpClient, mockConfig.Object);

            var result = await service.CheckContentAsync("Tanio", "Podejrzany opis", 50);

            result.IsSafe.Should().BeFalse();
            result.Reason.Should().Be("Scam detected");
        }
    }
}