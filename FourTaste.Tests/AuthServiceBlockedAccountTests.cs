using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class AuthServiceBlockedAccountTests
    {
        private readonly Mock<IUserRepository> userRepository = new();
        private readonly Mock<ICurrentUserSession> session = new();

        [Fact]
        public async Task LoginAsync_ShouldRefuseWithReasonAndNotSignIn_WhenAccountIsBlocked()
        {
            this.SetupUser(isBlocked: true, reason: "Спам");

            var result = await this.CreateService().LoginAsync("u@mail.com", "secret");

            Assert.True(result.IsFailure);
            Assert.StartsWith(AuthService.BlockedAccountError, result.Error);
            Assert.Contains("Спам", result.Error);
            this.session.Verify(s => s.SignIn(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldOmitReason_WhenBlockedWithoutReason()
        {
            this.SetupUser(isBlocked: true, reason: null);

            var result = await this.CreateService().LoginAsync("u@mail.com", "secret");

            Assert.Equal(AuthService.BlockedAccountError, result.Error);
        }

        [Fact]
        public async Task LoginAsync_ShouldNotRevealBlockedStatus_WhenPasswordIsWrong()
        {
            this.SetupUser(isBlocked: true, reason: "Спам");

            var result = await this.CreateService().LoginAsync("u@mail.com", "wrong-password");

            Assert.True(result.IsFailure);
            Assert.DoesNotContain(AuthService.BlockedAccountError, result.Error);
            this.session.Verify(s => s.SignIn(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldSignIn_WhenAccountIsNotBlocked()
        {
            this.SetupUser(isBlocked: false, reason: null);

            var result = await this.CreateService().LoginAsync("u@mail.com", "secret");

            Assert.True(result.IsSuccess);
            this.session.Verify(s => s.SignIn(It.IsAny<User>()), Times.Once);
        }

        private AuthService CreateService() =>
            new AuthService(this.userRepository.Object, this.session.Object, new Mock<ILogger>().Object);

        private void SetupUser(bool isBlocked, string? reason)
        {
            var user = new User
            {
                ID = 5,
                Email = "u@mail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret", 4),
                IsBlocked = isBlocked,
                BlockedReason = reason,
            };
            this.userRepository.Setup(r => r.GetByEmailAsync("u@mail.com")).ReturnsAsync(user);
        }
    }
}