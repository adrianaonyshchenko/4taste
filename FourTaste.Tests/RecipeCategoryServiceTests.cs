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
    public class RecipeCategoryServiceTests
    {
        [Fact]
        public async Task GetAllAsync_ShouldReturnCategoriesList_WhenCategoriesExist()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var expectedCategories = new List<RecipeCategory>
            {
                new RecipeCategory { Id = 1, Name = "Десерти" },
                new RecipeCategory { Id = 2, Name = "Сніданки" }
            };

            mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(expectedCategories);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.GetAllAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.Equal("Десерти", result.Value[0].Name);
            mockLogger.Verify(l => l.Information(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateCategory_WhenNameIsValidAndUnique()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            mockCategoryRepo.Setup(r => r.ExistsByNameAsync("Сніданки", null)).ReturnsAsync(false);
            mockCategoryRepo
                .Setup(r => r.AddAsync(It.IsAny<RecipeCategory>()))
                .Callback<RecipeCategory>(c => c.Id = 7)
                .Returns(Task.CompletedTask);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.CreateAsync("  Сніданки  ");

            Assert.True(result.IsSuccess);
            Assert.Equal(7, result.Value);
            mockCategoryRepo.Verify(
                r => r.AddAsync(It.Is<RecipeCategory>(c => c.Name == "Сніданки")),
                Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), "Сніданки", 7), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task CreateAsync_ShouldReturnFailure_WhenNameIsEmptyOrWhitespace(string? invalidName)
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.CreateAsync(invalidName!);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва категорії не може бути порожньою.", result.Error);
            mockCategoryRepo.Verify(r => r.AddAsync(It.IsAny<RecipeCategory>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnFailure_WhenNameExceedsMaxLength()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            string tooLongName = new string('а', RecipeCategoryService.MaxCategoryNameLength + 1);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.CreateAsync(tooLongName);

            Assert.True(result.IsFailure);
            Assert.Equal(
                $"Назва категорії не може перевищувати {RecipeCategoryService.MaxCategoryNameLength} символів.",
                result.Error);
            mockCategoryRepo.Verify(r => r.AddAsync(It.IsAny<RecipeCategory>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), tooLongName.Length), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnFailure_WhenCategoryAlreadyExists()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            mockCategoryRepo.Setup(r => r.ExistsByNameAsync("Десерти", null)).ReturnsAsync(true);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.CreateAsync("Десерти");

            Assert.True(result.IsFailure);
            Assert.Equal("Категорія «Десерти» уже існує.", result.Error);
            mockCategoryRepo.Verify(r => r.AddAsync(It.IsAny<RecipeCategory>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), "Десерти"), Times.Once);
        }

        [Fact]
        public async Task RenameAsync_ShouldUpdateCategoryAndRecipes_WhenValid()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var existing = new RecipeCategory { Id = 3, Name = "Сніданок" };
            mockCategoryRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
            mockCategoryRepo.Setup(r => r.ExistsByNameAsync("Сніданки", 3)).ReturnsAsync(false);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.RenameAsync(3, "Сніданки");

            Assert.True(result.IsSuccess);
            Assert.Equal("Сніданки", existing.Name);
            mockCategoryRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
            mockCategoryRepo.Verify(r => r.RenameCategoryInRecipesAsync("Сніданок", "Сніданки"), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 3, "Сніданок", "Сніданки"), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task RenameAsync_ShouldReturnFailure_WhenIdIsInvalid(int invalidId)
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.RenameAsync(invalidId, "Нова назва");

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор категорії.", result.Error);
            mockCategoryRepo.Verify(r => r.UpdateAsync(It.IsAny<RecipeCategory>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), invalidId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFailure_WhenRecipesExistInCategory()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var existing = new RecipeCategory { Id = 4, Name = "Салати" };
            mockCategoryRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);
            mockCategoryRepo.Setup(r => r.CountRecipesByCategoryNameAsync("Салати")).ReturnsAsync(3);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.DeleteAsync(4);

            Assert.True(result.IsFailure);
            Assert.Equal("Неможливо видалити категорію «Салати», оскільки до неї прив'язано рецептів: 3.", result.Error);
            mockCategoryRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), "Салати", 4, 3), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteCategory_WhenNoRecipesLinked()
        {
            var mockCategoryRepo = new Mock<IRecipeCategoryRepository>();
            var mockLogger = new Mock<ILogger>();

            var existing = new RecipeCategory { Id = 5, Name = "Напої" };
            mockCategoryRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
            mockCategoryRepo.Setup(r => r.CountRecipesByCategoryNameAsync("Напої")).ReturnsAsync(0);

            var service = new RecipeCategoryService(mockCategoryRepo.Object, mockLogger.Object);

            var result = await service.DeleteAsync(5);

            Assert.True(result.IsSuccess);
            mockCategoryRepo.Verify(r => r.DeleteAsync(5), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), "Напої", 5), Times.Once);
        }
    }
}