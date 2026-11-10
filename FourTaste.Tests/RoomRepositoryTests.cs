using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FourTaste.Tests
{
    public class RoomRepositoryTests
    {
        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

            public AppDbContext CreateDbContext() => new AppDbContext(_options);
        }

        private static (RoomRepository Repo, TestDbContextFactory Factory) CreateRepository()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var factory = new TestDbContextFactory(options);
            return (new RoomRepository(factory), factory);
        }

        [Fact]
        public async Task CreateRoomAsync_ShouldSaveRoomAndMembers()
        {
            var (repo, factory) = CreateRepository();

            var room = new Room
            {
                Name = "Сім'я Коваленків",
                Description = "Спільне меню на тиждень",
                OwnerId = 5,
                InviteCode = "K7W92X",
                CreatedAt = DateTime.UtcNow,
                Members = new List<RoomMember>
                {
                    new RoomMember
                    {
                        UserId = 5,
                        Role = RoomRole.Owner,
                        JoinedAt = DateTime.UtcNow
                    }
                }
            };

            var created = await repo.CreateRoomAsync(room);

            Assert.True(created.Id > 0);

            using var context = factory.CreateDbContext();
            var savedRoom = await context.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == created.Id);
            Assert.NotNull(savedRoom);
            Assert.Equal("Сім'я Коваленків", savedRoom.Name);
            Assert.Equal("K7W92X", savedRoom.InviteCode);
            Assert.Single(savedRoom.Members);
            Assert.Equal(5, savedRoom.Members[0].UserId);
            Assert.Equal(RoomRole.Owner, savedRoom.Members[0].Role);
        }

        [Fact]
        public async Task InviteCodeExistsAsync_ShouldReturnTrue_WhenCodeExists()
        {
            var (repo, factory) = CreateRepository();

            using (var context = factory.CreateDbContext())
            {
                context.Rooms.Add(new Room
                {
                    Name = "Тестова",
                    OwnerId = 1,
                    InviteCode = "ABC123",
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            bool exists = await repo.InviteCodeExistsAsync("ABC123");
            bool notExists = await repo.InviteCodeExistsAsync("NONEXIST");

            Assert.True(exists);
            Assert.False(notExists);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRoomWithMembers()
        {
            var (repo, factory) = CreateRepository();

            int roomId;
            using (var context = factory.CreateDbContext())
            {
                var room = new Room
                {
                    Name = "Кімната 1",
                    OwnerId = 2,
                    InviteCode = "XYZ789",
                    CreatedAt = DateTime.UtcNow,
                    Members = new List<RoomMember>
                    {
                        new RoomMember { UserId = 2, Role = RoomRole.Owner, JoinedAt = DateTime.UtcNow }
                    }
                };
                context.Rooms.Add(room);
                await context.SaveChangesAsync();
                roomId = room.Id;
            }

            var retrieved = await repo.GetByIdAsync(roomId);

            Assert.NotNull(retrieved);
            Assert.Equal("Кімната 1", retrieved.Name);
            Assert.Single(retrieved.Members);
        }

        [Fact]
        public async Task DeleteRoomAsync_ShouldRemoveRoomAndCascadeMembers()
        {
            var (repo, factory) = CreateRepository();

            int roomId;
            using (var context = factory.CreateDbContext())
            {
                var room = new Room
                {
                    Name = "Для видалення",
                    OwnerId = 3,
                    InviteCode = "DEL123",
                    CreatedAt = DateTime.UtcNow,
                    Members = new List<RoomMember>
                    {
                        new RoomMember { UserId = 3, Role = RoomRole.Owner, JoinedAt = DateTime.UtcNow }
                    }
                };
                context.Rooms.Add(room);
                await context.SaveChangesAsync();
                roomId = room.Id;
            }

            bool deleted = await repo.DeleteRoomAsync(roomId);

            Assert.True(deleted);

            using var verifyContext = factory.CreateDbContext();
            var exists = await verifyContext.Rooms.AnyAsync(r => r.Id == roomId);
            Assert.False(exists);
        }

        [Fact]
        public async Task GetUserRoomsAsync_ShouldReturnRoomsWhereUserIsOwnerOrMember()
        {
            var (repo, factory) = CreateRepository();

            using (var context = factory.CreateDbContext())
            {
                context.Rooms.AddRange(
                    new Room
                    {
                        Id = 1,
                        Name = "Власна кімната",
                        OwnerId = 10,
                        InviteCode = "OWN001",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        Members = new List<RoomMember> { new RoomMember { UserId = 10, Role = RoomRole.Owner } }
                    },
                    new Room
                    {
                        Id = 2,
                        Name = "Кімната як учасник",
                        OwnerId = 99,
                        InviteCode = "MEM002",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                        Members = new List<RoomMember> { new RoomMember { UserId = 10, Role = RoomRole.Member } }
                    },
                    new Room
                    {
                        Id = 3,
                        Name = "Чужа кімната",
                        OwnerId = 88,
                        InviteCode = "OTH003",
                        CreatedAt = DateTime.UtcNow,
                        Members = new List<RoomMember> { new RoomMember { UserId = 88, Role = RoomRole.Owner } }
                    });
                await context.SaveChangesAsync();
            }

            var userRooms = await repo.GetUserRoomsAsync(10);

            Assert.Equal(2, userRooms.Count);
            Assert.Contains(userRooms, r => r.Name == "Власна кімната");
            Assert.Contains(userRooms, r => r.Name == "Кімната як учасник");
            Assert.DoesNotContain(userRooms, r => r.Name == "Чужа кімната");
        }
    }
}
