using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.WPF;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
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
            var avatars = CreateAvatarService(CurrentUserMock.Anonymous(), out _);

            Assert.False(avatars.HasAvatar(0));
            Assert.False(avatars.HasAvatar(-1));
        }

        [Fact]
        public void AvatarService_LoadAvatar_ShouldReturnNull_WhenUserIdInvalid()
        {
            var avatars = CreateAvatarService(CurrentUserMock.Anonymous(), out _);

            Assert.Null(avatars.LoadAvatar(0));
            Assert.Null(avatars.LoadAvatar(-1));
        }

        [Fact]
        public void AvatarService_LoadAvatarBytes_ShouldReturnNull_WhenUserIdInvalid()
        {
            var avatars = CreateAvatarService(CurrentUserMock.Anonymous(), out _);

            Assert.Null(avatars.LoadAvatarBytes(0));
            Assert.Null(avatars.LoadAvatarBytes(-1));
        }

        [Fact]
        public void AvatarService_SaveAndDelete_SyncsCurrentUserFileAndDatabase()
        {
            const int testUserId = 8888;
            var currentUser = CurrentUserMock.Authenticated(testUserId);
            var sessionUser = currentUser.Object.CurrentUser!;
            var avatars = CreateAvatarService(currentUser, out var factory);
            using (var seed = factory.CreateDbContext())
            {
                seed.Users.Add(new User { ID = testUserId, Username = "tester", Email = "t@t.com", Name = "T", PasswordHash = "x" });
                seed.SaveChanges();
            }

            byte[] fakePng = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            Assert.True(avatars.SaveAvatar(testUserId, fakePng));
            Assert.Equal(fakePng, sessionUser.Avatar);
            Assert.True(File.Exists(avatars.GetAvatarFilePath(testUserId)));
            Assert.True(avatars.HasAvatar(testUserId));
            using (var check = factory.CreateDbContext())
            {
                Assert.Equal(fakePng, check.Users.Single(u => u.ID == testUserId).Avatar);
            }

            Assert.True(avatars.DeleteAvatar(testUserId));
            Assert.Null(sessionUser.Avatar);
            Assert.False(File.Exists(avatars.GetAvatarFilePath(testUserId)));
            using (var check = factory.CreateDbContext())
            {
                Assert.Null(check.Users.Single(u => u.ID == testUserId).Avatar);
            }
        }

        [Fact]
        public void AvatarService_LoadAvatarBytes_ShouldLoadOtherUserFromDatabaseAndCacheLocally()
        {
            byte[] stored = new byte[] { 1, 2, 3 };
            var avatars = CreateAvatarService(CurrentUserMock.Authenticated(1), out var factory);
            using (var seed = factory.CreateDbContext())
            {
                seed.Users.Add(new User { ID = 2, Username = "other", Email = "o@o.com", Name = "O", PasswordHash = "x", Avatar = stored });
                seed.SaveChanges();
            }

            Assert.Equal(stored, avatars.LoadAvatarBytes(2));
            Assert.Equal(stored, File.ReadAllBytes(avatars.GetAvatarFilePath(2)));
        }

        private static AvatarService CreateAvatarService(Mock<ICurrentUserService> currentUser, out IDbContextFactory<AppDbContext> factory)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            factory = new TestDbContextFactory(options);
            string directory = Path.Combine(Path.GetTempPath(), "FourTasteTests", Guid.NewGuid().ToString());
            return new AvatarService(currentUser.Object, factory, directory);
        }

        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => this.options = options;

            public AppDbContext CreateDbContext() => new AppDbContext(this.options);
        }
    }
}
