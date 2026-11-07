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
    public class RecipeEditTests
    {
        private readonly Mock<IRecipeRepository> _repositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFridgeRepository> _fridgeRepositoryMock;
        private readonly Mock<IAllergyService> _allergyServiceMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly RecipeService _service;

        public RecipeEditTests()
        {
            _repositoryMock = new Mock<IRecipeRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fridgeRepositoryMock = new Mock<IFridgeRepository>();
            _allergyServiceMock = new Mock<IAllergyService>();
            _loggerMock = new Mock<ILogger>();
            _service = new RecipeService(
                _repositoryMock.Object,
                _userRepositoryMock.Object,
                _fridgeRepositoryMock.Object,
                _allergyServiceMock.Object,
                _loggerMock.Object);
        }

        private static Recipe CreateSampleRecipe(int id = 1, int userId = 10) => new Recipe
        {
            Id = id,
            UserId = userId,
            Title = "Борщ",
            Time = "01:30",
            Category = "Супи та бульйони",
            Cuisine = "Українська",
            IsPublic = true,
            Servings = 4,
            Ingredients = new List<Ingredient>
            {
                new Ingredient { Id = 1, Name = "Буряк", Amount = "2 шт.", RecipeId = id },
            },
            Steps = new List<RecipeStep>
            {
                new RecipeStep { Id = 1, StepNumber = 1, Description = "Зварити бульйон", RecipeId = id },
            },
        };

        private static UpdateRecipeRequest CreateValidUpdateRequest(int id = 1, int userId = 10) => new UpdateRecipeRequest
        {
            Id = id,
            UserId = userId,
            Title = "Оновлений Борщ",
            Time = "01:45",
            Category = "Супи та бульйони",
            Cuisine = "Українська",
            IsPublic = false,
            Servings = 6,
            Ingredients = new List<RecipeIngredientDto>
            {
                new RecipeIngredientDto { Name = "Буряк", Amount = "3 шт." },
                new RecipeIngredientDto { Name = "Капуста", Amount = "300г." },
            },
            Steps = new List<string>
            {
                "Зварити бульйон",
                "Додати овочі",
            },
        };

        [Fact]
        public async Task UpdateRecipe_WhenRequestNull_ReturnsFailure()
        {
            var result = await _service.UpdateRecipeAsync(null!);

            Assert.True(result.IsFailure);
            Assert.Equal("Дані рецепта не задано.", result.Error);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRecipe_WhenRecipeNotFound_ReturnsFailure()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

            var request = CreateValidUpdateRequest(id: 999, userId: 10);
            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Рецепт не знайдено.", result.Error);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRecipe_WhenUserIsNotAuthor_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 99);
            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Ви можете редагувати лише власні рецепти.", result.Error);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRecipe_WhenTitleEmpty_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Title = "   ";

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва рецепта не може бути порожньою.", result.Error);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRecipe_WhenCategoryEmpty_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Category = string.Empty;

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть категорію рецепта.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipe_WhenCuisineEmpty_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Cuisine = "   ";

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Оберіть країну/кухню рецепта.", result.Error);
        }

        [Theory]
        [InlineData("00:00")]
        [InlineData("abc")]
        [InlineData("12")]
        [InlineData("01:65")]
        public async Task UpdateRecipe_WhenInvalidCookingTime_ReturnsFailure(string invalidTime)
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Time = invalidTime;

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Час приготування має бути у форматі 00:00 і бути більшим за нуль.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipe_WhenNoIngredients_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Ingredients.Clear();

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Додайте хоча б один інгредієнт.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipe_WhenIngredientMissingAmount_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Ingredients = new List<RecipeIngredientDto>
            {
                new RecipeIngredientDto { Name = "Сіль", Amount = "   " },
            };

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Вкажіть кількість для кожного інгредієнта.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipe_WhenNoSteps_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            request.Steps.Clear();

            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Додайте кроки приготування.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipe_WhenValid_UpdatesRecipeAndCallsRepository()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Recipe>())).Returns(Task.CompletedTask);

            var request = CreateValidUpdateRequest(id: 1, userId: 10);
            var result = await _service.UpdateRecipeAsync(request);

            Assert.True(result.IsSuccess);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Recipe>(rec =>
                rec.Id == 1 &&
                rec.Title == "Оновлений Борщ" &&
                rec.Time == "01:45" &&
                rec.Servings == 6 &&
                rec.IsPublic == false &&
                rec.Ingredients.Count == 2 &&
                rec.Steps.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task DeleteRecipe_WhenUserIsNotAuthor_ReturnsFailure()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

            var result = await _service.DeleteRecipeAsync(1, userId: 55);

            Assert.True(result.IsFailure);
            Assert.Equal("Ви можете видаляти лише власні рецепти.", result.Error);
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRecipe_WhenUserIsAuthor_CallsRepositoryDelete()
        {
            var recipe = CreateSampleRecipe(id: 1, userId: 10);
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);
            _repositoryMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

            var result = await _service.DeleteRecipeAsync(1, userId: 10);

            Assert.True(result.IsSuccess);
            _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteRecipe_WhenNotFound_ReturnsFailure()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Recipe?)null);

            var result = await _service.DeleteRecipeAsync(404, userId: 10);

            Assert.True(result.IsFailure);
            Assert.Equal("Рецепт не знайдено.", result.Error);
        }

        [Fact]
        public async Task CreateRecipe_WithIngredientItems_SavesIngredientsCorrectly()
        {
            Recipe? captured = null;
            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Recipe>()))
                .Callback<Recipe>(rec => captured = rec)
                .Returns(Task.CompletedTask);

            var request = new CreateRecipeRequest
            {
                Title = "Смачний суп",
                Time = "00:45",
                Category = "Супи та бульйони",
                Cuisine = "Українська",
                IsPublic = true,
                UserId = 10,
                IngredientItems = new List<RecipeIngredientDto>
                {
                    new RecipeIngredientDto { Name = "М'ясо", Amount = "500г." },
                    new RecipeIngredientDto { Name = "Картопля", Amount = "3 шт." },
                },
                Steps = new List<string> { "Зварити все" },
            };

            var result = await _service.CreateRecipeAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(captured);
            Assert.Equal(2, captured!.Ingredients.Count);
            Assert.Equal("М'ясо", captured.Ingredients[0].Name);
            Assert.Equal("500г.", captured.Ingredients[0].Amount);
            Assert.Equal("Картопля", captured.Ingredients[1].Name);
            Assert.Equal("3 шт.", captured.Ingredients[1].Amount);
        }
    }
}
