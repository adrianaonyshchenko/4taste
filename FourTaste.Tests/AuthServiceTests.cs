using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Serilog;
using FourTaste.ApplicationCore.Services;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.Tests
{
    public class AuthServiceTests
    {

        [Fact]
        public async Task LogoutAsync_ShouldCompleteSuccessfully_WhenUserExists()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
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
            var mockLogger = new Mock<ILogger>();

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
            var mockLogger = new Mock<ILogger>(); 
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LogoutAsync(invalidId));
        }

        [Fact]
        public async Task LoginAsync_Should_Return_User_When_Credentials_Are_Valid()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();

            var testUser = new User
            {
                ID = 1,
                Email = "test@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct_password")
            };

            mockRepo.Setup(r => r.GetByEmailAsync("test@gmail.com"))
                    .ReturnsAsync(testUser);

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LoginAsync("test@gmail.com", "correct_password");

            Assert.NotNull(result);
            Assert.Equal("test@gmail.com", result.Email);
        }

        [Fact]
        public async Task LoginAsync_Should_Throw_Exception_When_Password_Is_Wrong()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();

            var testUser = new User
            {
                ID = 1,
                Email = "test@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct_password")
            };

            mockRepo.Setup(r => r.GetByEmailAsync("test@gmail.com"))
                    .ReturnsAsync(testUser);

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginAsync("test@gmail.com", "wrong_password"));
        }

        [Fact]
        public async Task LoginAsync_Should_Throw_ArgumentException_When_Email_Is_Empty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.LoginAsync("", "some_password"));
        }
    }
}