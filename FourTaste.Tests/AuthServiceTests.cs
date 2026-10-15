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
        public async Task LogoutAsync_ShouldClearSessionAndNotQueryDatabase_WhenCalled()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            SessionContext.CurrentUser = new User { ID = 1, Username = "Tania" };
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LogoutAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Null(SessionContext.CurrentUser);
            mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_ShouldSucceedAndClearSession_WhenNoSessionActive()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            SessionContext.CurrentUser = null;
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LogoutAsync();

            Assert.True(result.IsSuccess);
            Assert.Null(SessionContext.CurrentUser);
            mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_ShouldLogInformation_WhenSessionIsActive()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            SessionContext.CurrentUser = new User { ID = 10, Username = "TestUser" };
            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LogoutAsync(10);

            Assert.True(result.IsSuccess);
            Assert.Null(SessionContext.CurrentUser);
            mockLogger.Verify(
                l => l.Information(
                    It.Is<string>(s => s.Contains("сесію очищено")),
                    10,
                    "TestUser"),
                Times.Once);
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

            Assert.True(result.IsSuccess);
            Assert.Equal("test@gmail.com", result.Value!.Email);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_Password_Is_Wrong()
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

            var result = await service.LoginAsync("test@gmail.com", "wrong_password");

            Assert.True(result.IsFailure);
            Assert.Equal("Невірна електронна пошта або пароль.", result.Error);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_Email_Is_Empty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LoginAsync(string.Empty, "some_password");

            Assert.True(result.IsFailure);
            Assert.Equal("Електронна пошта не може бути порожньою.", result.Error);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_Password_Is_Empty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LoginAsync("user@gmail.com", string.Empty);

            Assert.True(result.IsFailure);
            Assert.Equal("Пароль не може бути порожнім.", result.Error);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_User_Not_Found()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByEmailAsync("notfound@gmail.com")).ReturnsAsync((User?)null);
            var mockLogger = new Mock<ILogger>();

            var service = new AuthService(mockRepo.Object, mockLogger.Object);

            var result = await service.LoginAsync("notfound@gmail.com", "any_password");

            Assert.True(result.IsFailure);
            Assert.Equal("Невірна електронна пошта або пароль.", result.Error);
        }
    }
}