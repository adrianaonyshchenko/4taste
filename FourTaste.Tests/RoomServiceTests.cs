using System;
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
    [Collection("SessionContext")]
    public class RoomServiceTests : IDisposable
    {
        private const int CurrentUserId = 15;

        private readonly Mock<IRoomRepository> _roomRepositoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly RoomService _service;

        public RoomServiceTests()
        {
            _roomRepositoryMock = new Mock<IRoomRepository>();
            _loggerMock = new Mock<ILogger>();
            _service = new RoomService(_roomRepositoryMock.Object, _loggerMock.Object);

            SessionContext.CurrentUser = new User
            {
                ID = CurrentUserId,
                Name = "Тестовий Користувач",
                Username = "test_user",
                Email = "test@example.com"
            };
        }

        public void Dispose()
        {
            SessionContext.Clear();
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

            var result = await _service.CreateRoomAsync(request);

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

            var result = await _service.CreateRoomAsync(request);

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

            var result = await _service.CreateRoomAsync(request);

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

            var result = await _service.CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal($"Опис кімнати не може перевищувати {RoomOptions.MaxDescriptionLength} символів.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenUserIsNotLoggedIn()
        {
            SessionContext.Clear();

            var request = new CreateRoomRequest
            {
                Name = "Кімната"
            };

            var result = await _service.CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт, щоб створити кімнату.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenUserHasInvalidId(int userId)
        {
            SessionContext.CurrentUser = new User { ID = userId };

            var request = new CreateRoomRequest
            {
                Name = "Кімната"
            };

            var result = await _service.CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт, щоб створити кімнату.", result.Error);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Never);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenRequestIsNull()
        {
            var result = await _service.CreateRoomAsync(null!);

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

            var request = new CreateRoomRequest
            {
                Name = "Колізійна кімната"
            };

            var result = await _service.CreateRoomAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, checkCount);
            _roomRepositoryMock.Verify(r => r.CreateRoomAsync(It.IsAny<Room>()), Times.Once);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldReturnFailure_WhenRepositoryThrowsException()
        {
            _roomRepositoryMock.Setup(r => r.InviteCodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roomRepositoryMock.Setup(r => r.CreateRoomAsync(It.IsAny<Room>()))
                .ThrowsAsync(new InvalidOperationException("Помилка підключення до БД"));

            var request = new CreateRoomRequest
            {
                Name = "Кімната зі збоєм"
            };

            var result = await _service.CreateRoomAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Виникла технічна помилка при збереженні кімнати. Спробуйте пізніше.", result.Error);
        }

        [Fact]
        public async Task GetUserRoomsAsync_ShouldReturnRooms_WhenUserHasRooms()
        {
            var rooms = new System.Collections.Generic.List<Room>
            {
                new Room { Id = 1, Name = "Кімната 1", OwnerId = CurrentUserId, InviteCode = "AAA111", CreatedAt = DateTime.UtcNow },
                new Room { Id = 2, Name = "Кімната 2", OwnerId = CurrentUserId, InviteCode = "BBB222", CreatedAt = DateTime.UtcNow }
            };

            _roomRepositoryMock.Setup(r => r.GetUserRoomsAsync(CurrentUserId)).ReturnsAsync(rooms);

            var result = await _service.GetUserRoomsAsync(CurrentUserId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal("Кімната 1", result.Value[0].Name);
            Assert.Equal("Кімната 2", result.Value[1].Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetUserRoomsAsync_ShouldReturnEmptyList_WhenUserIdIsInvalid(int userId)
        {
            var result = await _service.GetUserRoomsAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value!);
            _roomRepositoryMock.Verify(r => r.GetUserRoomsAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
