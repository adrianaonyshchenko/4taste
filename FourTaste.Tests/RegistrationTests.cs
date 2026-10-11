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
            var mockRepo = new Mock<IUserRepository>();
            var service = new UserService(mockRepo.Object);

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

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateUser_WhenDataIsValid()
        {
            var existingUser = new User { ID = 1, Name = "Old", Username = "olduser", Email = "old@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var service = new UserService(mockRepo.Object);

            var result = await service.UpdateProfileAsync(1, "Софія", "sofiia", "sofiia@gmail.com");

            Assert.True(result);
            Assert.Equal("Софія", existingUser.Name);
            Assert.Equal("sofiia", existingUser.Username);
            Assert.Equal("sofiia@gmail.com", existingUser.Email);
            mockRepo.Verify(r => r.UpdateAsync(existingUser), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowArgumentException_WhenNewPasswordIsEmpty()
        {
            var mockRepo = new Mock<IUserRepository>();
            var service = new UserService(mockRepo.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ChangePasswordAsync(1, ""));
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldDeleteUser_WhenUserExists()
        {
            var existingUser = new User { ID = 1, Username = "user", Email = "user@gmail.com" };
            var mockRepo = new Mock<IUserRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingUser);
            var service = new UserService(mockRepo.Object);

            var result = await service.DeleteAccountAsync(1);

            Assert.True(result);
            mockRepo.Verify(r => r.DeleteAsync(existingUser), Times.Once);
        }
    }
}