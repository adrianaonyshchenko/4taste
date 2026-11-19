using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class RoomJoinTests
    {
        private readonly Mock<IRoomRepository> _roomRepositoryMock = new Mock<IRoomRepository>();
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

        private RoomService CreateServiceFor(int userId)
        {
            var currentUser = userId > 0 ? CurrentUserMock.Authenticated(userId) : CurrentUserMock.Anonymous();
            return new RoomService(_roomRepositoryMock.Object, currentUser.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task JoinRoomByCodeAsync_ValidCodeAndNewUser_ShouldSucceedAndAddMember()
        {
            string inviteCode = "ABC123XY";
            int userId = 5;
            int roomId = 1;

            var existingRoom = new Room
            {
                Id = roomId,
                Name = "Тестова кімната",
                OwnerId = 99,
                InviteCode = inviteCode,
                Members = new List<RoomMember>()
            };

            _roomRepositoryMock
                .Setup(r => r.GetByInviteCodeAsync(inviteCode))
                .ReturnsAsync(existingRoom);

            _roomRepositoryMock
          .Setup(r => r.AddMemberAsync(It.IsAny<RoomMember>()))
          .ReturnsAsync(true);

            var result = await CreateServiceFor(userId).JoinRoomByCodeAsync(inviteCode);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(roomId, result.Value.Id);
            Assert.Equal("Тестова кімната", result.Value.Name);

            _roomRepositoryMock.Verify(r => r.AddMemberAsync(It.Is<RoomMember>(m =>
                m.RoomId == roomId &&
                m.UserId == userId &&
                m.Role == RoomRole.Member
            )), Times.Once);
        }

        [Fact]
        public async Task JoinRoomByCodeAsync_InvalidUserId_ShouldReturnFailure()
        {
            int invalidUserId = 0;

            var result = await CreateServiceFor(invalidUserId).JoinRoomByCodeAsync("SOMECODE");

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт, щоб приєднатися до кімнати.", result.Error);
            _roomRepositoryMock.Verify(r => r.AddMemberAsync(It.IsAny<RoomMember>()), Times.Never);
        }

        [Fact]
        public async Task JoinRoomByCodeAsync_RoomNotFound_ShouldReturnFailure()
        {
            string inviteCode = "WRONGCODE";
            int userId = 3;

            _roomRepositoryMock
                .Setup(r => r.GetByInviteCodeAsync(inviteCode))
                .ReturnsAsync((Room?)null);

            var result = await CreateServiceFor(userId).JoinRoomByCodeAsync(inviteCode);

            Assert.True(result.IsFailure);
            Assert.Equal("Кімнату з таким кодом не знайдено.", result.Error);
        }

        [Fact]
        public async Task JoinRoomByCodeAsync_UserAlreadyMember_ShouldReturnFailure()
        {
            string inviteCode = "ABC123XY";
            int userId = 5;

            var existingRoom = new Room
            {
                Id = 1,
                Name = "Кімната",
                OwnerId = 2,
                InviteCode = inviteCode,
                Members = new List<RoomMember>
                {
                    new RoomMember { UserId = userId, Role = RoomRole.Member }
                }
            };

            _roomRepositoryMock
                .Setup(r => r.GetByInviteCodeAsync(inviteCode))
                .ReturnsAsync(existingRoom);

            var result = await CreateServiceFor(userId).JoinRoomByCodeAsync(inviteCode);

            Assert.True(result.IsFailure);
            Assert.Equal("Ви вже є учасником цієї кімнати.", result.Error);
            _roomRepositoryMock.Verify(r => r.AddMemberAsync(It.IsAny<RoomMember>()), Times.Never);
        }
    }
}