using System.Collections.Generic;
using System.Linq;
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
    public class ReviewServiceTests
    {
        private const int RecipeId = 1;
        private const int OwnerId = 5;
        private const int ReviewerId = 7;

        private readonly Mock<IReviewRepository> _reviewRepositoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ReviewService _service;

        public ReviewServiceTests()
        {
            _reviewRepositoryMock = new Mock<IReviewRepository>();
            _loggerMock = new Mock<ILogger>();
            _service = new ReviewService(_reviewRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnReviewId_WhenRequestIsValid()
        {
            SetupRecipe();
            _reviewRepositoryMock.Setup(r => r.ExistsAsync(RecipeId, ReviewerId)).ReturnsAsync(false);
            _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RecipeReview>())).ReturnsAsync(42);

            var result = await _service.AddReviewAsync(ValidRequest());

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
            _reviewRepositoryMock.Verify(
                r => r.AddAsync(It.Is<RecipeReview>(x => x.RecipeId == RecipeId && x.UserId == ReviewerId && x.Rating == 4 && x.Tags.Count == 1)),
                Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenUserIsNotLoggedIn(int userId)
        {
            var request = ValidRequest();
            request.UserId = userId;

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Увійдіть в акаунт, щоб залишити відгук.", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public async Task AddReviewAsync_ShouldReturnFailureWithoutDbCalls_WhenRatingIsOutOfRange(int rating)
        {
            var request = ValidRequest();
            request.Rating = rating;

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.GetRecipeAccessAsync(It.IsAny<int>()), Times.Never);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenRecipeIdIsInvalid(int recipeId)
        {
            var request = ValidRequest();
            request.RecipeId = recipeId;

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор рецепта.", result.Error);
            _reviewRepositoryMock.Verify(r => r.GetRecipeAccessAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenRecipeDoesNotExist()
        {
            _reviewRepositoryMock.Setup(r => r.GetRecipeAccessAsync(RecipeId)).ReturnsAsync((Recipe?)null);

            var result = await _service.AddReviewAsync(ValidRequest());

            Assert.True(result.IsFailure);
            Assert.Equal("Рецепт не знайдено.", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenReviewerIsRecipeOwner()
        {
            SetupRecipe();
            var request = ValidRequest();
            request.UserId = OwnerId;

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Не можна залишати відгук на власний рецепт.", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenRecipeIsPrivate()
        {
            SetupRecipe(isPublic: false);

            var result = await _service.AddReviewAsync(ValidRequest());

            Assert.True(result.IsFailure);
            Assert.Equal("Цей рецепт приватний.", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenReviewAlreadyExists()
        {
            SetupRecipe();
            _reviewRepositoryMock.Setup(r => r.ExistsAsync(RecipeId, ReviewerId)).ReturnsAsync(true);

            var result = await _service.AddReviewAsync(ValidRequest());

            Assert.True(result.IsFailure);
            Assert.Equal("Ви вже залишили відгук на цей рецепт.", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenTagIsUnknown()
        {
            var request = ValidRequest();
            request.Tags = new List<string> { "абракадабра" };

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            Assert.Contains("абракадабра", result.Error);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenTooManyTags()
        {
            var request = ValidRequest();
            request.Tags = ReviewOptions.Tags.Take(ReviewOptions.MaxTags + 1).ToList();

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenTextIsTooLong()
        {
            var request = ValidRequest();
            request.Text = new string('а', ReviewOptions.MaxTextLength + 1);

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldReturnFailure_WhenPhotoIsTooLarge()
        {
            var request = ValidRequest();
            request.Photo = new byte[ReviewOptions.MaxPhotoSizeBytes + 1];

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task AddReviewAsync_ShouldNormalizeTags_WhenTagsRepeatOrDifferInCase()
        {
            SetupRecipe();
            string tag = ReviewOptions.Tags[0];
            var request = ValidRequest();
            request.Tags = new List<string> { tag, tag.ToUpperInvariant() };
            _reviewRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RecipeReview>())).ReturnsAsync(1);

            var result = await _service.AddReviewAsync(request);

            Assert.True(result.IsSuccess);
            _reviewRepositoryMock.Verify(
                r => r.AddAsync(It.Is<RecipeReview>(x => x.Tags.Count == 1 && x.Tags[0].Name == tag)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldReturnSuccess_WhenReviewExists()
        {
            SetupRecipe();
            _reviewRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<RecipeReview>())).ReturnsAsync(true);
            var request = ValidRequest();
            request.Rating = 5;

            var result = await _service.UpdateReviewAsync(request);

            Assert.True(result.IsSuccess);
            _reviewRepositoryMock.Verify(
                r => r.UpdateAsync(It.Is<RecipeReview>(x => x.RecipeId == RecipeId && x.UserId == ReviewerId && x.Rating == 5)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldReturnFailure_WhenReviewDoesNotExist()
        {
            SetupRecipe();
            _reviewRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<RecipeReview>())).ReturnsAsync(false);

            var result = await _service.UpdateReviewAsync(ValidRequest());

            Assert.True(result.IsFailure);
            Assert.Equal("Відгук не знайдено.", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task UpdateReviewAsync_ShouldReturnFailureWithoutDbCalls_WhenRatingIsOutOfRange(int rating)
        {
            var request = ValidRequest();
            request.Rating = rating;

            var result = await _service.UpdateReviewAsync(request);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldReturnFailure_WhenRecipeIsPrivate()
        {
            SetupRecipe(isPublic: false);

            var result = await _service.UpdateReviewAsync(ValidRequest());

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<RecipeReview>()), Times.Never);
        }

        [Fact]
        public async Task DeleteReviewAsync_ShouldReturnSuccess_WhenReviewExists()
        {
            _reviewRepositoryMock.Setup(r => r.DeleteAsync(RecipeId, ReviewerId)).ReturnsAsync(true);

            var result = await _service.DeleteReviewAsync(RecipeId, ReviewerId);

            Assert.True(result.IsSuccess);
            _reviewRepositoryMock.Verify(r => r.DeleteAsync(RecipeId, ReviewerId), Times.Once);
        }

        [Fact]
        public async Task DeleteReviewAsync_ShouldReturnFailure_WhenReviewDoesNotExist()
        {
            _reviewRepositoryMock.Setup(r => r.DeleteAsync(RecipeId, ReviewerId)).ReturnsAsync(false);

            var result = await _service.DeleteReviewAsync(RecipeId, ReviewerId);

            Assert.True(result.IsFailure);
            Assert.Equal("Відгук не знайдено.", result.Error);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, -1)]
        public async Task DeleteReviewAsync_ShouldReturnFailureWithoutDbCalls_WhenIdsAreInvalid(int recipeId, int userId)
        {
            var result = await _service.DeleteReviewAsync(recipeId, userId);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetOverviewAsync_ShouldReturnReviews_WhenRecipeIsPublic()
        {
            SetupRecipe();
            _reviewRepositoryMock
                .Setup(r => r.GetByRecipeAsync(RecipeId))
                .ReturnsAsync(new List<RecipeReview> { new RecipeReview { Id = 1, Rating = 5 } });

            var result = await _service.GetOverviewAsync(RecipeId, null);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!.Reviews);
            Assert.False(result.Value.IsRecipeOwner);
        }

        [Fact]
        public async Task GetOverviewAsync_ShouldReturnFailure_WhenRecipeIsPrivateAndViewerIsGuest()
        {
            SetupRecipe(isPublic: false);

            var result = await _service.GetOverviewAsync(RecipeId, null);

            Assert.True(result.IsFailure);
            _reviewRepositoryMock.Verify(r => r.GetByRecipeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetOverviewAsync_ShouldMarkViewerAsOwner_WhenRecipeIsPrivateAndViewerIsOwner()
        {
            SetupRecipe(isPublic: false);
            _reviewRepositoryMock.Setup(r => r.GetByRecipeAsync(RecipeId)).ReturnsAsync(new List<RecipeReview>());

            var result = await _service.GetOverviewAsync(RecipeId, OwnerId);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.IsRecipeOwner);
        }

        [Fact]
        public async Task GetOverviewAsync_ShouldMarkViewerAsOwner_WhenRecipeIsPublicAndViewerIsOwner()
        {
            SetupRecipe();
            _reviewRepositoryMock.Setup(r => r.GetByRecipeAsync(RecipeId)).ReturnsAsync(new List<RecipeReview>());

            var result = await _service.GetOverviewAsync(RecipeId, OwnerId);

            Assert.True(result.Value!.IsRecipeOwner);
        }

        [Fact]
        public void ReviewsOverview_TagCounts_ShouldCountAndSortTags()
        {
            string first = ReviewOptions.Tags[0];
            string second = ReviewOptions.Tags[1];
            var overview = new ReviewsOverview
            {
                Reviews = new List<RecipeReview>
                {
                    new RecipeReview { Tags = new List<ReviewTag> { new ReviewTag { Name = second } } },
                    new RecipeReview { Tags = new List<ReviewTag> { new ReviewTag { Name = first }, new ReviewTag { Name = second } } },
                },
            };

            var counts = overview.TagCounts;

            Assert.Equal(2, counts.Count);
            Assert.Equal(second, counts[0].Key);
            Assert.Equal(2, counts[0].Value);
            Assert.Equal(first, counts[1].Key);
        }

        private static ReviewRequest ValidRequest()
        {
            return new ReviewRequest
            {
                RecipeId = RecipeId,
                UserId = ReviewerId,
                Rating = 4,
                Text = "Смачно",
                Tags = new List<string> { ReviewOptions.Tags[0] },
            };
        }

        private void SetupRecipe(bool isPublic = true)
        {
            _reviewRepositoryMock
                .Setup(r => r.GetRecipeAccessAsync(RecipeId))
                .ReturnsAsync(new Recipe { Id = RecipeId, UserId = OwnerId, IsPublic = isPublic });
        }
    }
}