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

            var result = await _service.CreateRecipeAsync(string.Empty, "00:15", "Десерти", "Українська", true, 1, ingredients, steps);

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
                new Recipe { Id = 2, Title = "Вареники", IsPublic = true },
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
                new Recipe { Id = 10, Title = "Мій секретний пиріг", UserId = userId },
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

        [Fact]
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
            var recipe = new Recipe { Id = 1, IsPublic = false, UserId = 5 };
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recipe);

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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchPublicRecipes_NullOrWhitespaceQuery_ReturnsAllPublicRecipes(string? query)
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 1 },
                new Recipe { Id = 2, Title = "Вареники", IsPublic = true, UserId = 2 },
            };
            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);

            var result = await _service.SearchPublicRecipesAsync(query);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task SearchPublicRecipes_ByTitle_MatchesCaseInsensitiveAndPartial()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Український борщ", IsPublic = true, UserId = 1 },
                new Recipe { Id = 2, Title = "Вареники з вишнею", IsPublic = true, UserId = 2 },
            };
            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);

            var result = await _service.SearchPublicRecipesAsync("бОРщ");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Український борщ", result.Value![0].Title);
        }

        [Fact]
        public async Task SearchPublicRecipes_ByAuthorName_MatchesAuthor()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 10 },
                new Recipe { Id = 2, Title = "Вареники", IsPublic = true, UserId = 20 },
            };
            var mockUsers = new List<User>
            {
                new User { ID = 10, Name = "Оксана Шевченко", Username = "oksana" },
                new User { ID = 20, Name = "Іван Франко", Username = "ivan" },
            };

            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);
            _userRepositoryMock.Setup(r => r.ListAsync()).ReturnsAsync(mockUsers);

            var result = await _service.SearchPublicRecipesAsync("Оксана");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Борщ", result.Value![0].Title);
            Assert.Equal("Оксана Шевченко", result.Value![0].AuthorName);
        }

        [Fact]
        public async Task SearchPublicRecipes_ByAuthorUsernameWithAtSign_MatchesAuthor()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 10 },
                new Recipe { Id = 2, Title = "Вареники", IsPublic = true, UserId = 20 },
            };
            var mockUsers = new List<User>
            {
                new User { ID = 10, Name = "Оксана Шевченко", Username = "oksana_chef" },
                new User { ID = 20, Name = "Іван Франко", Username = "ivan_cook" },
            };

            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);
            _userRepositoryMock.Setup(r => r.ListAsync()).ReturnsAsync(mockUsers);

            var result = await _service.SearchPublicRecipesAsync("@oksana_chef");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Борщ", result.Value![0].Title);
            Assert.Equal("oksana_chef", result.Value![0].AuthorUsername);
        }

        [Fact]
        public async Task SearchPublicRecipes_FilterTitleOnly_DoesNotMatchAuthor()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 10 },
            };
            var mockUsers = new List<User>
            {
                new User { ID = 10, Name = "Оксана", Username = "oksana" },
            };

            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);
            _userRepositoryMock.Setup(r => r.ListAsync()).ReturnsAsync(mockUsers);

            var result = await _service.SearchPublicRecipesAsync("Оксана", RecipeSearchFilter.TitleOnly);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task SearchPublicRecipes_FilterAuthorOnly_DoesNotMatchTitle()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 10 },
            };
            var mockUsers = new List<User>
            {
                new User { ID = 10, Name = "Оксана", Username = "oksana" },
            };

            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);
            _userRepositoryMock.Setup(r => r.ListAsync()).ReturnsAsync(mockUsers);

            var result = await _service.SearchPublicRecipesAsync("Борщ", RecipeSearchFilter.AuthorOnly);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Fact]
        public async Task SearchPublicRecipes_NoMatch_ReturnsEmptyList()
        {
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", IsPublic = true, UserId = 10 },
            };
            _repositoryMock.Setup(r => r.GetPublicRecipesAsync()).ReturnsAsync(mockRecipes);

            var result = await _service.SearchPublicRecipesAsync("Піца");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchUserRecipes_NullOrWhitespace_ReturnsAll(string? query)
        {
            int userId = 5;
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Пиріг", UserId = userId },
                new Recipe { Id = 2, Title = "Кекс", UserId = userId },
            };
            _repositoryMock.Setup(r => r.GetRecipesByUserIdAsync(userId)).ReturnsAsync(mockRecipes);

            var result = await _service.SearchUserRecipesAsync(userId, query);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task SearchUserRecipes_ByTitle_MatchesCaseInsensitive()
        {
            int userId = 5;
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Шоколадний пиріг", UserId = userId },
                new Recipe { Id = 2, Title = "Ванільний кекс", UserId = userId },
            };
            _repositoryMock.Setup(r => r.GetRecipesByUserIdAsync(userId)).ReturnsAsync(mockRecipes);

            var result = await _service.SearchUserRecipesAsync(userId, "ПИРІГ");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Шоколадний пиріг", result.Value![0].Title);
        }

        [Fact]
        public async Task SearchUserRecipes_ByCategoryOrCuisine_Matches()
        {
            int userId = 5;
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", Category = "Перші страви", Cuisine = "Українська", UserId = userId },
                new Recipe { Id = 2, Title = "Піца", Category = "Випічка", Cuisine = "Італійська", UserId = userId },
            };
            _repositoryMock.Setup(r => r.GetRecipesByUserIdAsync(userId)).ReturnsAsync(mockRecipes);

            var result = await _service.SearchUserRecipesAsync(userId, "Італійська");

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("Піца", result.Value![0].Title);
        }

        [Fact]
        public async Task SearchUserRecipes_NoMatch_ReturnsEmptyList()
        {
            int userId = 5;
            var mockRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", UserId = userId },
            };
            _repositoryMock.Setup(r => r.GetRecipesByUserIdAsync(userId)).ReturnsAsync(mockRecipes);

            var result = await _service.SearchUserRecipesAsync(userId, "Суші");

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }
    }
}
