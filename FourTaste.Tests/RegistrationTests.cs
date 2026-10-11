using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FourTaste.ApplicationCore.Services;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.Tests
{
    public class RegistrationTests
    {
        [Fact]
        public async Task RegisterAsync_ShouldThrowArgumentException_WhenRequiredFieldsAreEmpty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var service = new UserService(mockRepo.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.RegisterAsync("", "", ""));
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowFormatException_WhenEmailIsInvalid()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var service = new UserService(mockRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() =>
                service.RegisterAsync("username", "invalid-email", "password123"));
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnTrue_WhenDataIsValid()
        {
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            var service = new UserService(mockRepo.Object);

            var result = await service.RegisterAsync("newuser", "new@gmail.com", "securepassword123");

            Assert.True(result);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        }
    }
}