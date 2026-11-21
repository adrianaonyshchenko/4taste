using System;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FourTaste.Tests
{
    public class UserWarningRepositoryTests
    {
        [Fact]
        public async Task GetUnreadAsync_ShouldReturnOnlyOwnUnreadWarnings_NewestFirst()
        {
            var factory = CreateFactory();
            using (var context = factory.CreateDbContext())
            {
                context.UserWarnings.AddRange(
                    new UserWarning { Id = 1, UserId = 7, Message = "стара", CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new UserWarning { Id = 2, UserId = 7, Message = "нова", CreatedAt = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc) },
                    new UserWarning { Id = 3, UserId = 7, Message = "прочитана", IsRead = true, CreatedAt = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc) },
                    new UserWarning { Id = 4, UserId = 8, Message = "чужа", CreatedAt = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc) });
                context.SaveChanges();
            }

            var result = await new UserWarningRepository(factory).GetUnreadAsync(7);

            Assert.Equal(new[] { 2, 1 }, result.Select(w => w.Id).ToArray());
        }

        [Fact]
        public async Task MarkAllReadAsync_ShouldMarkOnlyThisUsersWarnings()
        {
            var factory = CreateFactory();
            using (var context = factory.CreateDbContext())
            {
                context.UserWarnings.AddRange(
                    new UserWarning { Id = 1, UserId = 7, Message = "a", CreatedAt = DateTime.UtcNow },
                    new UserWarning { Id = 2, UserId = 8, Message = "b", CreatedAt = DateTime.UtcNow });
                context.SaveChanges();
            }

            var repository = new UserWarningRepository(factory);
            await repository.MarkAllReadAsync(7);

            Assert.Empty(await repository.GetUnreadAsync(7));
            Assert.Single(await repository.GetUnreadAsync(8));
        }

        [Fact]
        public async Task AddAsync_ShouldPersistWarning()
        {
            var factory = CreateFactory();
            var repository = new UserWarningRepository(factory);

            await repository.AddAsync(new UserWarning { UserId = 7, AdminId = 1, Message = "Текст", CreatedAt = DateTime.UtcNow });

            var warning = Assert.Single(await repository.GetUnreadAsync(7));
            Assert.Equal("Текст", warning.Message);
            Assert.Equal(1, warning.AdminId);
        }

        private static TestDbContextFactory CreateFactory() =>
            new TestDbContextFactory(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => this.options = options;

            public AppDbContext CreateDbContext() => new AppDbContext(this.options);
        }
    }
}