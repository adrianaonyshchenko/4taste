using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class UserModerationTests
    {
        private const int AdminId = 1;
        private const int TargetId = 5;
        private const int OtherAdminId = 9;

        private readonly Mock<IUserRepository> userRepository = new();
        private readonly Mock<IUserWarningRepository> warningRepository = new();

        [Theory]
        [InlineData("block")]
        [InlineData("unblock")]
        [InlineData("warning")]
        [InlineData("delete")]
        [InlineData("view")]
        public async Task Action_ShouldBeDeniedWithoutDbCalls_WhenUserIsNotAdmin(string action)
        {
            var result = await Run(this.CreateService(userId: 7), action, TargetId);

            Assert.True(result.IsFailure);
            Assert.Equal(AdminService.AccessDeniedError, result.Error);
            this.userRepository.VerifyNoOtherCalls();
            this.warningRepository.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("block")]
        [InlineData("unblock")]
        [InlineData("warning")]
        [InlineData("delete")]
        public async Task ModerationAction_ShouldRefuseWithoutDbCalls_WhenAdminTargetsThemselves(string action)
        {
            var result = await Run(this.CreateAdminService(), action, AdminId);

            Assert.True(result.IsFailure);
            Assert.Equal(UserModerationService.SelfActionError, result.Error);
            this.userRepository.VerifyNoOtherCalls();
            this.warningRepository.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("block")]
        [InlineData("unblock")]
        [InlineData("warning")]
        [InlineData("delete")]
        public async Task ModerationAction_ShouldRefuse_WhenTargetIsAnotherAdmin(string action)
        {
            this.SetupUser(OtherAdminId, UserRole.Admin);

            var result = await Run(this.CreateAdminService(), action, OtherAdminId);

            Assert.True(result.IsFailure);
            Assert.Equal(UserModerationService.AdminTargetError, result.Error);
            this.AssertNothingWritten();
        }

        [Theory]
        [InlineData("block")]
        [InlineData("unblock")]
        [InlineData("warning")]
        [InlineData("delete")]
        [InlineData("view")]
        public async Task Action_ShouldReturnNotFound_WhenUserDoesNotExist(string action)
        {
            this.userRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((User?)null);

            var result = await Run(this.CreateAdminService(), action, 404);

            Assert.True(result.IsFailure);
            Assert.Equal(UserModerationService.UserNotFoundError, result.Error);
            this.AssertNothingWritten();
        }

        [Theory]
        [InlineData("block")]
        [InlineData("unblock")]
        [InlineData("warning")]
        [InlineData("delete")]
        [InlineData("view")]
        public async Task Action_ShouldFailWithoutRepositoryCalls_WhenIdIsInvalid(string action)
        {
            var result = await Run(this.CreateAdminService(), action, 0);

            Assert.True(result.IsFailure);
            this.userRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BlockUserAsync_ShouldBlockAndTrimReason()
        {
            var user = this.SetupUser(TargetId);

            var result = await this.CreateAdminService().BlockUserAsync(TargetId, "  Спам у рецептах  ");

            Assert.True(result.IsSuccess);
            Assert.True(user.IsBlocked);
            Assert.Equal("Спам у рецептах", user.BlockedReason);
            this.userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BlockUserAsync_ShouldStoreNullReason_WhenReasonIsBlank(string reason)
        {
            var user = this.SetupUser(TargetId);

            var result = await this.CreateAdminService().BlockUserAsync(TargetId, reason);

            Assert.True(result.IsSuccess);
            Assert.Null(user.BlockedReason);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldFail_WhenAlreadyBlocked()
        {
            this.SetupUser(TargetId, isBlocked: true);

            var result = await this.CreateAdminService().BlockUserAsync(TargetId, null);

            Assert.True(result.IsFailure);
            this.AssertNothingWritten();
        }

        [Fact]
        public async Task BlockUserAsync_ShouldFailWithoutDbCalls_WhenReasonIsTooLong()
        {
            var result = await this.CreateAdminService().BlockUserAsync(TargetId, new string('а', UserModerationService.MaxReasonLength + 1));

            Assert.True(result.IsFailure);
            this.userRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldClearBlockAndReason()
        {
            var user = this.SetupUser(TargetId, isBlocked: true);
            user.BlockedReason = "Спам";

            var result = await this.CreateAdminService().UnblockUserAsync(TargetId);

            Assert.True(result.IsSuccess);
            Assert.False(user.IsBlocked);
            Assert.Null(user.BlockedReason);
            this.userRepository.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldFail_WhenUserIsNotBlocked()
        {
            this.SetupUser(TargetId);

            var result = await this.CreateAdminService().UnblockUserAsync(TargetId);

            Assert.True(result.IsFailure);
            this.AssertNothingWritten();
        }

        [Fact]
        public async Task SendWarningAsync_ShouldStoreTrimmedUnreadWarning()
        {
            this.SetupUser(TargetId);

            var result = await this.CreateAdminService().SendWarningAsync(TargetId, "  Не публікуйте образливі відгуки  ");

            Assert.True(result.IsSuccess);
            this.warningRepository.Verify(
                r => r.AddAsync(It.Is<UserWarning>(w =>
                    w.UserId == TargetId && w.AdminId == AdminId && w.Message == "Не публікуйте образливі відгуки" && !w.IsRead)),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendWarningAsync_ShouldFailWithoutDbCalls_WhenMessageIsEmpty(string message)
        {
            var result = await this.CreateAdminService().SendWarningAsync(TargetId, message);

            Assert.True(result.IsFailure);
            this.userRepository.VerifyNoOtherCalls();
            this.warningRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendWarningAsync_ShouldFail_WhenMessageIsTooLong()
        {
            var result = await this.CreateAdminService().SendWarningAsync(TargetId, new string('а', UserModerationService.MaxWarningLength + 1));

            Assert.True(result.IsFailure);
            this.warningRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldDeleteUser()
        {
            var user = this.SetupUser(TargetId);

            var result = await this.CreateAdminService().DeleteUserAsync(TargetId);

            Assert.True(result.IsSuccess);
            this.userRepository.Verify(r => r.DeleteAsync(user), Times.Once);
        }

        [Fact]
        public async Task GetUserAsync_ShouldReturnUser_ForAdmin()
        {
            var user = this.SetupUser(TargetId);

            var result = await this.CreateAdminService().GetUserAsync(TargetId);

            Assert.True(result.IsSuccess);
            Assert.Same(user, result.Value);
        }

        [Fact]
        public async Task GetMyUnreadWarningsAsync_ShouldReturnOnlyCurrentUsersWarnings()
        {
            var warnings = new List<UserWarning> { new UserWarning { Id = 1, UserId = 7 } };
            this.warningRepository.Setup(r => r.GetUnreadAsync(7)).ReturnsAsync(warnings);

            var result = await this.CreateService(userId: 7).GetMyUnreadWarningsAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task MarkMyWarningsReadAsync_ShouldMarkOnlyCurrentUsersWarnings()
        {
            var result = await this.CreateService(userId: 7).MarkMyWarningsReadAsync();

            Assert.True(result.IsSuccess);
            this.warningRepository.Verify(r => r.MarkAllReadAsync(7), Times.Once);
        }

        [Theory]
        [InlineData("unread")]
        [InlineData("markread")]
        public async Task MyWarnings_ShouldFailWithoutDbCalls_WhenNobodySignedIn(string operation)
        {
            var service = this.CreateService(userId: 0, isAuthenticated: false);

            var result = operation == "unread"
                ? ToResult(await service.GetMyUnreadWarningsAsync())
                : await service.MarkMyWarningsReadAsync();

            Assert.True(result.IsFailure);
            this.warningRepository.VerifyNoOtherCalls();
        }

        private static async Task<Result> Run(UserModerationService service, string action, int id)
        {
            switch (action)
            {
                case "block":
                    return await service.BlockUserAsync(id, null);
                case "unblock":
                    return await service.UnblockUserAsync(id);
                case "warning":
                    return await service.SendWarningAsync(id, "Текст");
                case "delete":
                    return await service.DeleteUserAsync(id);
                case "view":
                    var view = await service.GetUserAsync(id);
                    return view.IsSuccess ? Result.Success() : Result.Failure(view.Error!);
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        private static Result ToResult<T>(Result<T> result) =>
            result.IsSuccess ? Result.Success() : Result.Failure(result.Error!);

        private UserModerationService CreateAdminService() => this.CreateService(userId: AdminId, isAdmin: true);

        private UserModerationService CreateService(int userId, bool isAdmin = false, bool isAuthenticated = true)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(c => c.UserId).Returns(userId);
            currentUser.SetupGet(c => c.IsAdmin).Returns(isAdmin);
            currentUser.SetupGet(c => c.IsAuthenticated).Returns(isAuthenticated);

            return new UserModerationService(
                this.userRepository.Object,
                this.warningRepository.Object,
                currentUser.Object,
                new Mock<ILogger>().Object);
        }

        private User SetupUser(int id, UserRole role = UserRole.User, bool isBlocked = false)
        {
            var user = new User { ID = id, Username = "user" + id, Role = role, IsBlocked = isBlocked };
            this.userRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
            return user;
        }

        private void AssertNothingWritten()
        {
            this.userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
            this.userRepository.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
            this.warningRepository.Verify(r => r.AddAsync(It.IsAny<UserWarning>()), Times.Never);
        }
    }
}