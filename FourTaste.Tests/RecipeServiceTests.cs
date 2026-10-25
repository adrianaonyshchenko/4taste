using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;

namespace FourTaste.Tests
{
    public class RecipeListTests
    {
        private readonly Mock<IRecipeRepository> _repositoryMock;
        private readonly Mock<ILogger<RecipeService>> _loggerMock;
        private readonly RecipeService _service;

        public RecipeListTests()
        {
            _repositoryMock = new Mock<IRecipeRepository>();
            _loggerMock = new Mock<ILogger<RecipeService>>();
            _service = new RecipeService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateRecipe_WithEmptyTitle_ReturnsFailureResult()
        {
            var ingredients = new List<string> { "Цукор" };
            var steps = new List<string> { "Змішати інгредієнти" };

            var result = await _service.CreateRecipeAsync("", "00:15", "Десерти", "Українська", true, 1, ingredients, steps);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва рецепта не може бути порожньою.", result.Error);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task CreateRecipe_WithValidData_ReturnsSuccessResult()
        {
            var ingredients = new List<string> { "Борошно", "Вода" };
            var steps = new List<string> { "Замісити тісто", "Випекти" };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Recipe>())).Returns(Task.CompletedTask);

            var result = await _service.CreateRecipeAsync("Хліб", "01:00", "Випічка", "Українська", true, 1, ingredients, steps);

            Assert.True(result.IsSuccess);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Once);
        }

        [Fact]
        public async Task GetPublicRecipes_ReturnsSuccessWithList()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true },
                new Recipe { Id = 2, Title = "Вареники", IsPublic = true }
            };

            _repositoryMock.Setup(r => r.GetPublicRecipesAsync())
                .ReturnsAsync(mockRecipes);

            var result = await _service.GetPublicRecipesAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal("Борщ", result.Value[0].Title);
            _repositoryMock.Verify(r => r.GetPublicRecipesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetRecipesByUserId_ReturnsSuccessWithUserRecipes()
        {
            int userId = 5;
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 10, Title = "Мій секретний пиріг", UserId = userId }
            };

            _repositoryMock.Setup(r => r.GetRecipesByUserIdAsync(userId))
                .ReturnsAsync(mockRecipes);

            var result = await _service.GetRecipesByUserIdAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal(userId, result.Value[0].UserId);
            Assert.Equal("Мій секретний пиріг", result.Value[0].Title);
            _repositoryMock.Verify(r => r.GetRecipesByUserIdAsync(userId), Times.Once);
        }
    }
}