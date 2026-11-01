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

        private static CreateRecipeRequest ValidRequest() => new()
        {
            Title = "Хліб",
            Time = "01:00",
            Category = "Випічка",
            Cuisine = "Українська",
            IsPublic = true,
            UserId = 1,
            Ingredients = new List<string> { "Борошно", "Вода" },
            Steps = new List<string> { "Замісити тісто", "Випекти" },
        };

        [Fact]
        public async Task CreateRecipe_WithEmptyTitle_ReturnsFailureResult()
        {
            var request = ValidRequest();
            request.Title = string.Empty;

            var result = await _service.CreateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва рецепта не може бути порожньою.", result.Error);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task CreateRecipe_NullRequest_ReturnsFailure()
        {
            var result = await _service.CreateRecipeAsync(null!);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task CreateRecipe_WithValidData_ReturnsSuccessResult()
        {
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Recipe>())).Returns(Task.CompletedTask);

            var result = await _service.CreateRecipeAsync(ValidRequest());

            Assert.True(result.IsSuccess);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Once);
        }

        [Fact]
        public async Task CreateRecipe_WithTooLargePhoto_ReturnsFailure()
        {
            var request = ValidRequest();
            request.Photo = new byte[(5 * 1024 * 1024) + 1];

            var result = await _service.CreateRecipeAsync(request);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task CreateRecipe_WithNoIngredients_ReturnsFailure()
        {
            var request = ValidRequest();
            request.Ingredients = new List<string>();

            var result = await _service.CreateRecipeAsync(request);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task CreateRecipe_WithNoSteps_ReturnsFailure()
        {
            var request = ValidRequest();
            request.Steps = new List<string>();

            var result = await _service.CreateRecipeAsync(request);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task SearchRecipes_PassesCriteriaToRepository_AndReturnsItsResult()
        {
            var criteria = RecipeSearchCriteria.Public("борщ", RecipeSearchFilter.TitleOnly);
            var recipes = new List<Recipe> { new Recipe { Id = 1, Title = "Борщ", IsPublic = true } };
            _repositoryMock.Setup(r => r.SearchAsync(criteria)).ReturnsAsync(recipes);

            var result = await _service.SearchRecipesAsync(criteria);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            _repositoryMock.Verify(r => r.SearchAsync(criteria), Times.Once);
        }

        [Fact]
        public async Task SearchRecipes_ForUser_BuildsOwnerCriteria()
        {
            RecipeSearchCriteria? captured = null;
            _repositoryMock.Setup(r => r.SearchAsync(It.IsAny<RecipeSearchCriteria>()))
                .Callback<RecipeSearchCriteria>(c => captured = c)
                .ReturnsAsync(new List<Recipe>());

            var result = await _service.SearchRecipesAsync(RecipeSearchCriteria.ForUser(5, "пиріг"));

            Assert.True(result.IsSuccess);
            Assert.NotNull(captured);
            Assert.Equal(5, captured!.OwnerUserId);
            Assert.False(captured.OnlyPublic);
            Assert.Equal("пиріг", captured.SearchTerm);
        }

        [Fact]
        public async Task SearchRecipes_NullCriteria_ReturnsFailureWithoutCallingRepository()
        {
            var result = await _service.SearchRecipesAsync(null!);

            Assert.True(result.IsFailure);
            _repositoryMock.Verify(r => r.SearchAsync(It.IsAny<RecipeSearchCriteria>()), Times.Never);
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