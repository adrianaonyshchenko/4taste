using System;
using System.IO;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.WPF;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    [Collection("SessionContext")]
    public class UserAvatarTests
    {
        [Fact]
        public async Task UpdateAvatarAsync_ShouldUpdateUserAvatar_WhenUserExists()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            var user = new User { ID = 1, Name = "Тест", Email = "test@example.com" };
            mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var service = new UserService(mockRepo.Object, mockLogger.Object);
            byte[] avatarBytes = new byte[] { 1, 2, 3, 4, 5 };

            var result = await service.UpdateAvatarAsync(1, avatarBytes);

            Assert.True(result.IsSuccess);
            Assert.Equal(avatarBytes, user.Avatar);
            mockRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UpdateAvatarAsync_ShouldReturnFailure_WhenUserDoesNotExist()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var service = new UserService(mockRepo.Object, mockLogger.Object);
            var result = await service.UpdateAvatarAsync(99, new byte[] { 1 });

            Assert.True(result.IsFailure);
            Assert.Equal("Користувача не знайдено.", result.Error);
        }

        [Fact]
        public async Task GetAvatarAsync_ShouldReturnAvatarBytes_WhenUserExists()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            byte[] avatarBytes = new byte[] { 10, 20, 30 };
            var user = new User { ID = 2, Avatar = avatarBytes };
            mockRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);

            var service = new UserService(mockRepo.Object, mockLogger.Object);
            var result = await service.GetAvatarAsync(2);

            Assert.True(result.IsSuccess);
            Assert.Equal(avatarBytes, result.Value);
        }

        [Fact]
        public async Task GetAvatarAsync_ShouldReturnFailure_WhenUserDoesNotExist()
        {
            var mockRepo = new Mock<IUserRepository>();
            var mockLogger = new Mock<ILogger>();
            mockRepo.Setup(r => r.GetByIdAsync(100)).ReturnsAsync((User?)null);

            var service = new UserService(mockRepo.Object, mockLogger.Object);
            var result = await service.GetAvatarAsync(100);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public void AvatarService_HasAvatar_ShouldReturnFalse_WhenUserIdInvalid()
        {
            Assert.False(AvatarService.HasAvatar(0));
            Assert.False(AvatarService.HasAvatar(-1));
        }

        [Fact]
        public void AvatarService_LoadAvatar_ShouldReturnNull_WhenUserIdInvalid()
        {
            Assert.Null(AvatarService.LoadAvatar(0));
            Assert.Null(AvatarService.LoadAvatar(-1));
        }

        [Fact]
        public void AvatarService_SessionSync_UpdatesSessionContext()
        {
            int testUserId = 8888;
            var testUser = new User { ID = testUserId, Username = "tester" };
            SessionContext.CurrentUser = testUser;

            try
            {
                byte[] fakePng = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
                bool saved = AvatarService.SaveAvatar(testUserId, fakePng);

                Assert.True(saved);
                Assert.NotNull(testUser.Avatar);
                Assert.Equal(fakePng, testUser.Avatar);
                Assert.True(AvatarService.HasAvatar(testUserId));

                bool deleted = AvatarService.DeleteAvatar(testUserId);
                Assert.True(deleted);
                Assert.Null(testUser.Avatar);
            }
            finally
            {
                SessionContext.Clear();
                string path = AvatarService.GetAvatarFilePath(testUserId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
