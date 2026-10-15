using System;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class RegistrationTests
    {
        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenRequiredFieldsAreEmpty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.RegisterAsync(string.Empty, string.Empty, string.Empty);

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal("Email та пароль не можуть бути порожніми.", result.Error);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenEmailIsInvalid()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.RegisterAsync("username", "invalid-email", "password123");

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal("Некоректний формат електронної пошти.", result.Error);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
        {
            var existingUser = new User { ID = 1, Email = "existing@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByEmailAsync("existing@gmail.com")).ReturnsAsync(existingUser);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.RegisterAsync("user", "existing@gmail.com", "pass");

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal("Користувач з таким email вже існує.", result.Error);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnTrue_WhenDataIsValid()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.RegisterAsync("newuser", "new@gmail.com", "securepassword123", "New Name");

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Null(result.Error);
            mockRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "new@gmail.com" && u.Username == "newuser" && u.Name == "New Name")), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), "new@gmail.com"), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserExists()
        {
            var existingUser = new User { ID = 1, Name = "User", Username = "user", Email = "user@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var service = new UserService(mockRepo.Object);

            var result = await service.GetUserByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(existingUser, result.Value);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnFailure_WhenUserDoesNotExist()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
            var service = new UserService(mockRepo.Object);

            var result = await service.GetUserByIdAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal("Користувача не знайдено.", result.Error);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateUser_WhenDataIsValid()
        {
            var existingUser = new User { ID = 1, Name = "Old", Username = "olduser", Email = "old@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.UpdateProfileAsync(1, "Софія", "sofiia", "sofiia@gmail.com");

            Assert.True(result.IsSuccess);
            Assert.Equal("Софія", existingUser.Name);
            Assert.Equal("sofiia", existingUser.Username);
            Assert.Equal("sofiia@gmail.com", existingUser.Email);
            mockRepo.Verify(r => r.UpdateAsync(existingUser), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 1), Times.Once);
        }

        [Theory]
        [InlineData("", "valid@gmail.com", "Нікнейм та електронна пошта не можуть бути порожніми.")]
        [InlineData("valid_username", "", "Нікнейм та електронна пошта не можуть бути порожніми.")]
        [InlineData("valid_username", "invalid_email_format", "Некоректний формат електронної пошти.")]
        public async Task UpdateProfileAsync_ShouldReturnFailure_WhenDataIsInvalid(string username, string email, string expectedError)
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.UpdateProfileAsync(1, "Name", username, email);

            Assert.True(result.IsFailure);
            Assert.Equal(expectedError, result.Error);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.UpdateProfileAsync(999, "Name", "username", "user@gmail.com");

            Assert.True(result.IsFailure);
            Assert.Equal("Користувача не знайдено.", result.Error);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnFailure_WhenNewPasswordIsEmpty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.ChangePasswordAsync(1, string.Empty);

            Assert.True(result.IsFailure);
            Assert.Equal("Новий пароль не може бути порожнім.", result.Error);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.ChangePasswordAsync(999, "newpass123");

            Assert.True(result.IsFailure);
            Assert.Equal("Користувача не знайдено.", result.Error);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldSucceed_WhenPasswordIsValid()
        {
            var existingUser = new User { ID = 1, Username = "user", PasswordHash = "old_hash" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.ChangePasswordAsync(1, "new_secret_pass");

            Assert.True(result.IsSuccess);
            Assert.NotEqual("old_hash", existingUser.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("new_secret_pass", existingUser.PasswordHash));
            mockRepo.Verify(r => r.UpdateAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteUser_WhenUserExists()
        {
            var existingUser = new User { ID = 1, Username = "user", Email = "user@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.DeleteAccountAsync(1);

            Assert.True(result.IsSuccess);
            mockRepo.Verify(r => r.DeleteAsync(existingUser), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 1), Times.Once);
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldReturnFailure_WhenUserNotFound()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
            var mockLogger = new Mock<ILogger>();
            var service = new UserService(mockRepo.Object, mockLogger.Object);

            var result = await service.DeleteAccountAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal("Користувача не знайдено.", result.Error);
            mockRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
        }
    }
}