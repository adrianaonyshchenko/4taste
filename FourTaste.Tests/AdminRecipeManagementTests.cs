using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class AdminRecipeManagementTests
    {
        private readonly Mock<IRecipeRepository> _recipeRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IFridgeRepository> _fridgeRepositoryMock;
        private readonly Mock<IFavoriteRepository> _favoriteRepositoryMock;
        private readonly Mock<IAllergyService> _allergyServiceMock;
        private readonly Mock<IImageThumbnailGenerator> _thumbnailGeneratorMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly RecipeService _recipeService;

        public AdminRecipeManagementTests()
        {
            _recipeRepositoryMock = new Mock<IRecipeRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _fridgeRepositoryMock = new Mock<IFridgeRepository>();
            _favoriteRepositoryMock = new Mock<IFavoriteRepository>();
            _allergyServiceMock = new Mock<IAllergyService>();
            _thumbnailGeneratorMock = new Mock<IImageThumbnailGenerator>();
            _loggerMock = new Mock<ILogger>();

            _recipeService = new RecipeService(
                _recipeRepositoryMock.Object,
                _userRepositoryMock.Object,
                _fridgeRepositoryMock.Object,
                _favoriteRepositoryMock.Object,
                _allergyServiceMock.Object,
                _thumbnailGeneratorMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task SearchRecipesAsync_AdminCriteriaWithOnlyPublicFalse_ShouldReturnPublicAndPrivateRecipes()
        {
            var criteria = new RecipeSearchCriteria
            {
                SearchTerm = "Борщ",
                Filter = RecipeSearchFilter.TitleOnly,
                OnlyPublic = false,
            };

            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ український", IsPublic = true, UserId = 10 },
                new Recipe { Id = 2, Title = "Борщ домашній приватний", IsPublic = false, UserId = 12 },
            };

            _recipeRepositoryMock
                .Setup(r => r.SearchAsync(It.Is<RecipeSearchCriteria>(c =>
                    !c.OnlyPublic &&
                    c.SearchTerm == "Борщ" &&
                    c.Filter == RecipeSearchFilter.TitleOnly)))
                .ReturnsAsync(expectedRecipes);

            var result = await _recipeService.SearchRecipesAsync(criteria);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public async Task SearchRecipesAsync_FilterByAuthor_ShouldPassAuthorFilterToRepository()
        {
            var criteria = new RecipeSearchCriteria
            {
                SearchTerm = "adriana",
                Filter = RecipeSearchFilter.AuthorOnly,
                OnlyPublic = false,
            };

            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 5, Title = "Сирники", AuthorUsername = "adriana", IsPublic = false, UserId = 7 },
            };

            _recipeRepositoryMock
                .Setup(r => r.SearchAsync(It.Is<RecipeSearchCriteria>(c =>
                    c.Filter == RecipeSearchFilter.AuthorOnly &&
                    c.SearchTerm == "adriana" &&
                    !c.OnlyPublic)))
                .ReturnsAsync(expectedRecipes);

            var result = await _recipeService.SearchRecipesAsync(criteria);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.Equal("adriana", result.Value![0].AuthorUsername);
        }

        [Fact]
        public async Task GetRecipeDetailsAsync_PrivateRecipeViewedByAdmin_ShouldReturnSuccess()
        {
            int adminId = 1;
            int authorId = 25;
            int recipeId = 100;

            var privateRecipe = new Recipe
            {
                Id = recipeId,
                Title = "Секретний рецепт",
                IsPublic = false,
                UserId = authorId,
                AuthorUsername = "author_user",
            };

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(privateRecipe);

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(adminId))
                .ReturnsAsync(new User { ID = adminId, Role = UserRole.Admin });

            var result = await _recipeService.GetRecipeDetailsAsync(recipeId, adminId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(recipeId, result.Value.Recipe.Id);
            Assert.False(result.Value.Recipe.IsPublic);
        }

        [Fact]
        public async Task GetRecipeDetailsAsync_PrivateRecipeViewedByNonOwnerRegularUser_ShouldReturnFailure()
        {
            int otherUserId = 2;
            int authorId = 25;
            int recipeId = 100;

            var privateRecipe = new Recipe
            {
                Id = recipeId,
                Title = "Секретний рецепт",
                IsPublic = false,
                UserId = authorId,
            };

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(privateRecipe);

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(otherUserId))
                .ReturnsAsync(new User { ID = otherUserId, Role = UserRole.User });

            var result = await _recipeService.GetRecipeDetailsAsync(recipeId, otherUserId);

            Assert.True(result.IsFailure);
            Assert.Equal("Цей рецепт приватний.", result.Error);
        }

        [Fact]
        public async Task UpdateRecipeAsync_AdminEditingOtherUsersRecipe_ShouldSucceedAndUpdateFields()
        {
            int adminId = 1;
            int authorId = 15;
            int recipeId = 42;

            var existingRecipe = new Recipe
            {
                Id = recipeId,
                UserId = authorId,
                Title = "Стара назва",
                Time = "00:20",
                Category = "Сніданки",
                Cuisine = "Українська",
                IsPublic = false,
                Servings = 2,
                Ingredients = new List<Ingredient> { new Ingredient { Name = "Яйця", Amount = "2 шт." } },
                Steps = new List<RecipeStep> { new RecipeStep { StepNumber = 1, Description = "Збити яйця" } },
            };

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(existingRecipe);

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(adminId))
                .ReturnsAsync(new User { ID = adminId, Role = UserRole.Admin });

            _recipeRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Recipe>()))
                .Returns(Task.CompletedTask);

            var updateRequest = new UpdateRecipeRequest
            {
                Id = recipeId,
                UserId = adminId,
                Title = "Оновлена адміном назва",
                Time = "00:35",
                Category = "Основні страви",
                Cuisine = "Українська",
                IsPublic = true,
                Servings = 4,
                UpdatePhoto = false,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new RecipeIngredientDto { Name = "Яйця", Amount = "3 шт." },
                },
                Steps = new List<string>
                {
                    "Збити яйця|WHY:Для пишності",
                    "Посмажити на пательні",
                },
            };

            var result = await _recipeService.UpdateRecipeAsync(updateRequest);

            Assert.True(result.IsSuccess);
            Assert.Equal("Оновлена адміном назва", existingRecipe.Title);
            Assert.Equal("00:35", existingRecipe.Time);
            Assert.True(existingRecipe.IsPublic);
            Assert.Equal(2, existingRecipe.Steps.Count);
            _recipeRepositoryMock.Verify(r => r.UpdateAsync(existingRecipe), Times.Once);
        }

        [Fact]
        public async Task UpdateRecipeAsync_RegularUserEditingOtherUsersRecipe_ShouldReturnFailure()
        {
            int regularUserId = 2;
            int authorId = 15;
            int recipeId = 42;

            var existingRecipe = new Recipe
            {
                Id = recipeId,
                UserId = authorId,
                Title = "Чужий рецепт",
            };

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(existingRecipe);

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(regularUserId))
                .ReturnsAsync(new User { ID = regularUserId, Role = UserRole.User });

            var updateRequest = new UpdateRecipeRequest
            {
                Id = recipeId,
                UserId = regularUserId,
                Title = "Спроба зміни",
                Time = "00:30",
                Category = "Десерти",
                Cuisine = "Французька",
                Servings = 1,
                Ingredients = new List<RecipeIngredientDto> { new RecipeIngredientDto { Name = "Цукор", Amount = "50 г" } },
                Steps = new List<string> { "Змішати" },
            };

            var result = await _recipeService.UpdateRecipeAsync(updateRequest);

            Assert.True(result.IsFailure);
            Assert.Equal("Ви можете редагувати лише власні рецепти.", result.Error);
            _recipeRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRecipeAsync_AdminDeletingOtherUsersRecipe_ShouldSucceed()
        {
            int adminId = 1;
            int authorId = 30;
            int recipeId = 77;

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(new Recipe { Id = recipeId, UserId = authorId });

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(adminId))
                .ReturnsAsync(new User { ID = adminId, Role = UserRole.Admin });

            _recipeRepositoryMock
                .Setup(r => r.DeleteAsync(recipeId))
                .Returns(Task.CompletedTask);

            var result = await _recipeService.DeleteRecipeAsync(recipeId, adminId);

            Assert.True(result.IsSuccess);
            _recipeRepositoryMock.Verify(r => r.DeleteAsync(recipeId), Times.Once);
        }

        [Fact]
        public async Task DeleteRecipeAsync_RegularUserDeletingOtherUsersRecipe_ShouldReturnFailure()
        {
            int regularUserId = 2;
            int authorId = 30;
            int recipeId = 77;

            _recipeRepositoryMock
                .Setup(r => r.GetByIdAsync(recipeId))
                .ReturnsAsync(new Recipe { Id = recipeId, UserId = authorId });

            _userRepositoryMock
                .Setup(u => u.GetByIdAsync(regularUserId))
                .ReturnsAsync(new User { ID = regularUserId, Role = UserRole.User });

            var result = await _recipeService.DeleteRecipeAsync(recipeId, regularUserId);

            Assert.True(result.IsFailure);
            Assert.Equal("Ви можете видаляти лише власні рецепти.", result.Error);
            _recipeRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}