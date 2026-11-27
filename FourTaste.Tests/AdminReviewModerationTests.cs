using System;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class AdminReviewModerationTests
    {
        private readonly Mock<IReviewRepository> reviewRepository = new();

        [Fact]
        public async Task DeleteReviewByAdminAsync_ShouldDelete_WhenCallerIsAdmin()
        {
            this.reviewRepository.Setup(r => r.DeleteByIdAsync(42)).ReturnsAsync(true);

            var result = await this.CreateService(CurrentUserMock.Authenticated(1, isAdmin: true))
                .DeleteReviewByAdminAsync(42);

            Assert.True(result.IsSuccess);
            this.reviewRepository.Verify(r => r.DeleteByIdAsync(42), Times.Once);
        }

        [Fact]
        public async Task DeleteReviewByAdminAsync_ShouldDenyWithoutDbCalls_WhenCallerIsNotAdmin()
        {
            var result = await this.CreateService(CurrentUserMock.Authenticated(7, isAdmin: false))
                .DeleteReviewByAdminAsync(42);

            Assert.True(result.IsFailure);
            Assert.Equal(ReviewService.AccessDeniedError, result.Error);
            this.reviewRepository.Verify(r => r.DeleteByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task DeleteReviewByAdminAsync_ShouldFailWithoutDbCalls_WhenReviewIdIsInvalid(int invalidId)
        {
            var result = await this.CreateService(CurrentUserMock.Authenticated(1, isAdmin: true))
                .DeleteReviewByAdminAsync(invalidId);

            Assert.True(result.IsFailure);
            this.reviewRepository.Verify(r => r.DeleteByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteReviewByAdminAsync_ShouldFail_WhenReviewDoesNotExist()
        {
            this.reviewRepository.Setup(r => r.DeleteByIdAsync(404)).ReturnsAsync(false);

            var result = await this.CreateService(CurrentUserMock.Authenticated(1, isAdmin: true))
                .DeleteReviewByAdminAsync(404);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task ReviewRepository_DeleteByIdAsync_ShouldRemoveReviewAndItsTags()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var factory = new TestDbContextFactory(options);

            using (var setup = factory.CreateDbContext())
            {
                setup.RecipeReviews.Add(new RecipeReview
                {
                    Id = 10,
                    RecipeId = 3,
                    UserId = 7,
                    Rating = 2,
                    Text = "Образливий текст",
                    Tags = { new ReviewTag { Name = "Занадто солоне" } },
                });
                setup.SaveChanges();
            }

            var repo = new ReviewRepository(factory);
            bool deleted = await repo.DeleteByIdAsync(10);

            Assert.True(deleted);
            Assert.Empty(await repo.GetByRecipeAsync(3));
        }

        private ReviewService CreateService(Mock<ICurrentUserService> currentUser) =>
            new(this.reviewRepository.Object, currentUser.Object, new Mock<ILogger>().Object);

        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> options;
            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => this.options = options;
            public AppDbContext CreateDbContext() => new(this.options);
        }
    }
}