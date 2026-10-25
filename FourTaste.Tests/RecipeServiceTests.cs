using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
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
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly RecipeService _service;

        public RecipeListTests()
        {
            _repositoryMock = new Mock<IRecipeRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger>();
            _service = new RecipeService(_repositoryMock.Object, _userRepositoryMock.Object, _loggerMock.Object);
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
        public async Task CreateRecipe_WithTooLargePhoto_ReturnsFailure()
        {
            var photo = new byte[(5 * 1024 * 1024) + 1];

            var result = await _service.CreateRecipeAsync(
                "Хліб", "01:00", "Випічка", "Українська", true, 1,
                new List<string> { "Борошно" }, new List<string> { "Замісити" }, photo);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task GetRecipeDetails_PublicRecipe_ReturnsSuccessForGuest()
        {
            var recipe = new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 5 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new User { ID = 5, Username = "chef" });

            var result = await _service.GetRecipeDetailsAsync(1, null);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Борщ", result.Value.Recipe.Title);
            Assert.Equal("chef", result.Value.AuthorName);
        }

        [Fact]
        public async Task GetRecipeDetails_RecipeNotFound_ReturnsFailure()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

            var result = await _service.GetRecipeDetailsAsync(999, 1);

            Assert.True(result.IsFailure);
            Assert.Equal("Рецепт не знайдено.", result.Error);
        }

        [Fact]
        public async Task GetRecipeDetails_PrivateRecipeOfAnotherUser_ReturnsFailure()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Recipe { Id = 1, IsPublic = false, UserId = 5 });

            var result = await _service.GetRecipeDetailsAsync(1, 7);

            Assert.True(result.IsFailure);
            Assert.Equal("Цей рецепт приватний.", result.Error);
        }

        [Fact]
        public async Task GetRecipeDetails_PrivateRecipeForGuest_ReturnsFailure()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Recipe { Id = 1, IsPublic = false, UserId = 5 });

            var result = await _service.GetRecipeDetailsAsync(1, null);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task GetRecipeDetails_PrivateRecipeOfAuthor_ReturnsSuccess()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Recipe { Id = 1, IsPublic = false, UserId = 5 });
            _userRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new User { ID = 5, Username = "chef" });

            var result = await _service.GetRecipeDetailsAsync(1, 5);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetRecipeDetails_AuthorDeleted_UsesFallbackName()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Recipe { Id = 1, IsPublic = true, UserId = 5 });
            _userRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((User?)null);

            var result = await _service.GetRecipeDetailsAsync(1, null);

            Assert.True(result.IsSuccess);
            Assert.Equal("Невідомий автор", result.Value!.AuthorName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetRecipeDetails_InvalidId_ReturnsFailureWithoutCallingRepository(int invalidId)
        {
            var result = await _service.GetRecipeDetailsAsync(invalidId, 1);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}