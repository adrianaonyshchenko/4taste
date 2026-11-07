using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class RecipeFavoriteTests
    {
        [Fact]
        public async Task ToggleFavoriteAsync_ShouldAddFavorite_WhenNotYetFavorite()
        {
            var mockRecipeRepo = new Mock<IRecipeRepository>();
            var mockUserRepo = new Mock<IUserRepository>();
            var mockFridgeRepo = new Mock<IFridgeRepository>();
            var mockFavoriteRepo = new Mock<IFavoriteRepository>();
            var mockAllergyService = new Mock<IAllergyService>();
            var mockLogger = new Mock<ILogger>();

            var recipe = new Recipe { Id = 10, Title = "Тестовий рецепт", UserId = 1 };
            mockRecipeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(recipe);
            mockFavoriteRepo.Setup(r => r.IsFavoriteAsync(1, 10)).ReturnsAsync(false);

            var service = new RecipeService(
                mockRecipeRepo.Object,
                mockUserRepo.Object,
                mockFridgeRepo.Object,
                mockFavoriteRepo.Object,
                mockAllergyService.Object,
                mockLogger.Object
                );

            var result = await service.ToggleFavoriteAsync(1, 10);

            Assert.True(result.IsSuccess);
            mockFavoriteRepo.Verify(r => r.AddFavoriteAsync(1, 10), Times.Once);
            mockFavoriteRepo.Verify(r => r.RemoveFavoriteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 1, 10), Times.Once);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldRemoveFavorite_WhenAlreadyFavorite()
        {

            var mockRecipeRepo = new Mock<IRecipeRepository>();
            var mockUserRepo = new Mock<IUserRepository>();
            var mockFridgeRepo = new Mock<IFridgeRepository>();
            var mockFavoriteRepo = new Mock<IFavoriteRepository>();
            var mockAllergyService = new Mock<IAllergyService>();
            var mockLogger = new Mock<ILogger>();

            var recipe = new Recipe { Id = 10, Title = "Тестовий рецепт", UserId = 1 };
            mockRecipeRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(recipe);
            mockFavoriteRepo.Setup(r => r.IsFavoriteAsync(1, 10)).ReturnsAsync(true);

            var service = new RecipeService(
                mockRecipeRepo.Object,
                mockUserRepo.Object,
                mockFridgeRepo.Object,
                mockFavoriteRepo.Object,
                mockAllergyService.Object,
                mockLogger.Object);

            var result = await service.ToggleFavoriteAsync(1, 10);

            Assert.True(result.IsSuccess);
            mockFavoriteRepo.Verify(r => r.RemoveFavoriteAsync(1, 10), Times.Once);
            mockFavoriteRepo.Verify(r => r.AddFavoriteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 1, 10), Times.Once);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(1, 0)]
        [InlineData(-1, 5)]
        public async Task ToggleFavoriteAsync_ShouldReturnFailure_WhenIdsAreInvalid(int userId, int recipeId)
        {

            var mockRecipeRepo = new Mock<IRecipeRepository>();
            var mockUserRepo = new Mock<IUserRepository>();
            var mockFridgeRepo = new Mock<IFridgeRepository>();
            var mockFavoriteRepo = new Mock<IFavoriteRepository>();
            var mockAllergyService = new Mock<IAllergyService>();
            var mockLogger = new Mock<ILogger>();

            var service = new RecipeService(
                mockRecipeRepo.Object,
                mockUserRepo.Object,
                mockFridgeRepo.Object,
                mockFavoriteRepo.Object,
                mockAllergyService.Object,
                mockLogger.Object);

            var result = await service.ToggleFavoriteAsync(userId, recipeId);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректні дані запиту.", result.Error);
            mockFavoriteRepo.Verify(r => r.AddFavoriteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), userId, recipeId), Times.Once);
        }

        [Fact]
        public async Task GetFavoriteRecipesAsync_ShouldReturnRecipesList_WhenUserIdIsValid()
        {
            var mockRecipeRepo = new Mock<IRecipeRepository>();
            var mockUserRepo = new Mock<IUserRepository>();
            var mockFridgeRepo = new Mock<IFridgeRepository>();
            var mockFavoriteRepo = new Mock<IFavoriteRepository>();
            var mockAllergyService = new Mock<IAllergyService>();
            var mockLogger = new Mock<ILogger>();

            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Млиняники" },
                new Recipe { Id = 2, Title = "Борщ" }
            };

            mockFavoriteRepo.Setup(r => r.GetFavoriteRecipesByUserIdAsync(1)).ReturnsAsync(expectedRecipes);

            var service = new RecipeService(
                mockRecipeRepo.Object,
                mockUserRepo.Object,
                mockFridgeRepo.Object,
                mockFavoriteRepo.Object,
                mockAllergyService.Object,
                mockLogger.Object);

            var result = await service.GetFavoriteRecipesAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal("Млинці", result.Value[0].Title);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 2, 1), Times.Once);
        }
    }
}