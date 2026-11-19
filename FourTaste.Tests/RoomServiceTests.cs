using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class RoomServiceTests
    {
        private const int CurrentUserId = 15;

        private readonly Mock<IRoomRepository> _roomRepositoryMock = new Mock<IRoomRepository>();
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();

        private RoomService CreateService(Mock<ICurrentUserService>? currentUser = null)
        {
            currentUser ??= CurrentUserMock.Authenticated(CurrentUserId);
            return new RoomService(_roomRepositoryMock.Object, currentUser.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldCreateRoomAndReturnDto_WhenRequestIsValid()
        {
            _roomRepositoryMock.Setup(r => r.InviteCodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roomRepositoryMock.Setup(r => r.CreateRoomAsync(It.IsAny<Room>())).ReturnsAsync((Room r) =>
            {
                r.Id = 42;
                return r;
            });

            var request = new CreateRoomRequest
            {
                Name = "Затишна кухня",
                Description = "Кімната для сімейного меню"
            };

            var result = await CreateService().CreateRoomAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(42, result.Value!.Id);
            Assert.Equal("Затишна кухня", result.Value.Name);
            Assert.Equal("Кімната для сімейного меню", result.Value.Description);
            Assert.Equal(CurrentUserId, result.Value.OwnerId);
            Assert.Equal(RoomOptions.InviteCodeLength, result.Value.InviteCode.Length);

            _roomRepositoryMock.Verify(
                r => r.CreateRoomAsync(It.Is<Room>(rm =>
                    rm.Name == "Затишна кухня" &&
                    rm.Description == "Кімната для сімейного меню" &&
                    rm.OwnerId == CurrentUserId &&
                    rm.Members.Count == 1 &&
                    rm.Members[0].UserId == CurrentUserId &&
                    rm.Members[0].Role == RoomRole.Owner)),
                Times.Once);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldTrimNameAndStoreNullDescription_WhenDescriptionIsWhitespace()
        {
            _roomRepositoryMock.Setup(r => r.InviteCodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roomRepositoryMock.Setup(r => r.CreateRoomAsync(It.IsAny<Room>())).ReturnsAsync((Room r) => r);

            var result = await CreateService().CreateRoomAsync(new CreateRoomRequest { Name = "  Кухня  ", Description = "   " });

            Assert.True(result.IsSuccess);
            Assert.Equal("Кухня", result.Value!.Name);
            Assert.Null(result.Value.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenNameIsEmptyOrWhitespace(string? name)
        {
            var request = new CreateRoomRequest
            {
                Name = name!,
                Description = "Опис"
            };

            var result = await CreateService().CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва кімнати не може бути порожньою.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenNameExceedsMaxLength()
        {
            var request = new CreateRoomRequest
            {
                Name = new string('A', RoomOptions.MaxNameLength + 1)
            };

            var result = await CreateService().CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal($"Назва кімнати не може перевищувати {RoomOptions.MaxNameLength} символів.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenDescriptionExceedsMaxLength()
        {
            var request = new CreateRoomRequest
            {
                Name = "Нормальна назва",
                Description = new string('D', RoomOptions.MaxDescriptionLength + 1)
            };

            var result = await CreateService().CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal($"Опис кімнати не може перевищувати {RoomOptions.MaxDescriptionLength} символів.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenUserIsNotLoggedIn()
        {
            var service = CreateService(CurrentUserMock.Anonymous());

            var result = await service.CreateRoomAsync(new CreateRoomRequest { Name = "Кімната" });

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт, щоб створити кімнату.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenRequestIsNull()
        {
            var result = await CreateService().CreateRoomAsync(null!);

            Assert.True(result.IsFailure);
            Assert.Equal("Дані кімнати не задано.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldRetryAndGenerateNewCode_WhenInviteCodeCollides()
        {
            int checkCount = 0;
            _roomRepositoryMock.Setup(r => r.InviteCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(() =>
                {
                    checkCount++;
                    return checkCount == 1;
                });

            _roomRepositoryMock.Setup(r => r.CreateRoomAsync(It.IsAny<Room>())).ReturnsAsync((Room r) =>
            {
                r.Id = 100;
                return r;
            });

            var result = await CreateService().CreateRoomAsync(new CreateRoomRequest { Name = "Колізійна кімната" });

            Assert.True(result.IsSuccess);
            Assert.Equal(2, checkCount);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Once);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldPropagateException_WhenRepositoryThrows()
        {
            _roomRepositoryMock.Setup(r => r.InviteCodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roomRepositoryMock.Setup(r => r.CreateRoomAsync(It.IsAny<Room>()))
                .ThrowsAsync(new InvalidOperationException("Помилка підключення до БД"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService().CreateRoomAsync(new CreateRoomRequest { Name = "Кімната зі збоєм" }));
        }

        [Fact]
        public async Task GetUserRoomsAsync_ShouldReturnRoomsOfCurrentUser()
        {
            var rooms = new List<Room>
            {
                new Room { Id = 1, Name = "Кімната 1", OwnerId = CurrentUserId, InviteCode = "AAA111", CreatedAt = DateTime.UtcNow },
                new Room { Id = 2, Name = "Кімната 2", OwnerId = CurrentUserId, InviteCode = "BBB222", CreatedAt = DateTime.UtcNow }
            };

            _roomRepositoryMock.Setup(r => r.GetUserRoomsAsync(CurrentUserId)).ReturnsAsync(rooms);

            var result = await CreateService().GetUserRoomsAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal("Кімната 1", result.Value[0].Name);
            Assert.Equal("Кімната 2", result.Value[1].Name);
        }

        [Fact]
        public async Task GetUserRoomsAsync_ShouldReturnEmptyList_WhenUserIsNotLoggedIn()
        {
            var result = await CreateService(CurrentUserMock.Anonymous()).GetUserRoomsAsync();

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
            _roomRepositoryMock.Verify(r => r.GetUserRoomsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetRoomDetailsAsync_ShouldMarkCurrentUserAsOwner()
        {
            var room = new Room
            {
                Id = 3,
                Name = "Кімната",
                OwnerId = CurrentUserId,
                InviteCode = "CCC333",
                Members = new List<RoomMember> { new RoomMember { UserId = CurrentUserId, Role = RoomRole.Owner } },
            };
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(room);
            _roomRepositoryMock.Setup(r => r.GetUserNamesAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, string> { [CurrentUserId] = "Соня" });

            var result = await CreateService().GetRoomDetailsAsync(3);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.IsOwner);
            Assert.Equal("Соня", result.Value.Members[0].Name);
            Assert.Equal("Власник", result.Value.Members[0].Role);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldDelete_WhenCurrentUserIsOwner()
        {
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Room { Id = 5, OwnerId = CurrentUserId });

            var result = await CreateService().DeleteRoomAsync(5);

            Assert.True(result.IsSuccess);
            _roomRepositoryMock.Verify(r => r.DeleteRoomAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldFail_WhenCurrentUserIsNotOwner()
        {
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Room { Id = 5, OwnerId = 99 });

            var result = await CreateService().DeleteRoomAsync(5);

            Assert.True(result.IsFailure);
            Assert.Equal("Тільки власник може видалити кімнату.", result.Error);
            _roomRepositoryMock.Verify(r => r.DeleteRoomAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldFail_WhenRoomNotFound()
        {
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Room?)null);

            var result = await CreateService().DeleteRoomAsync(5);

            Assert.True(result.IsFailure);
            Assert.Equal("Кімнату не знайдено.", result.Error);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldFail_WhenUserIsNotLoggedIn()
        {
            var result = await CreateService(CurrentUserMock.Anonymous()).DeleteRoomAsync(5);

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт.", result.Error);
            _roomRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LeaveRoomAsync_ShouldRemoveCurrentUser_WhenUserIsMember()
        {
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Room { Id = 7, OwnerId = 99 });

            var result = await CreateService().LeaveRoomAsync(7);

            Assert.True(result.IsSuccess);
            _roomRepositoryMock.Verify(r => r.RemoveMemberAsync(7, CurrentUserId), Times.Once);
        }

        [Fact]
        public async Task LeaveRoomAsync_ShouldFail_WhenCurrentUserIsOwner()
        {
            _roomRepositoryMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new Room { Id = 7, OwnerId = CurrentUserId });

            var result = await CreateService().LeaveRoomAsync(7);

            Assert.True(result.IsFailure);
            Assert.Equal("Власник не може покинути кімнату. Її можна тільки видалити.", result.Error);
            _roomRepositoryMock.Verify(r => r.RemoveMemberAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
