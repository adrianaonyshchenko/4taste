using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FourTaste.ApplicationCore.Services;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Entities;
using Microsoft.Extensions.Logging;

namespace FourTaste.Tests
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task LogoutAsync_ShouldCompleteSuccessfully_WhenUserExists()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger<AuthService>>();
            var existingUser = new User { ID = 1, Username = "Tania" };
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var exception = await Record.ExceptionAsync(() => service.LogoutAsync(1));

            Assert.Null(exception);
        }

        [Fact]
        public async Task LogoutAsync_ShouldThrowArgumentException_WhenUserDoesNotExist()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger<AuthService>>();
            mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LogoutAsync(999));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task LogoutAsync_ShouldThrowArgumentException_WhenUserIdIsInvalid(int invalidId)
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger<AuthService>>();
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LogoutAsync(invalidId));
        }
    }
}