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
    public class ShoppingListServiceTests
    {
        private readonly Mock<IShoppingListRepository> _shoppingRepoMock;
        private readonly Mock<IRoomMealPlanRepository> _mealPlanRepoMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ShoppingListService _service;

        public ShoppingListServiceTests()
        {
            _shoppingRepoMock = new Mock<IShoppingListRepository>();
            _mealPlanRepoMock = new Mock<IRoomMealPlanRepository>();
            _loggerMock = new Mock<ILogger>();
            _service = new ShoppingListService(
                _shoppingRepoMock.Object,
                _mealPlanRepoMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GenerateShoppingList_WithValidRoomIdAndRecipes_ReturnsSuccessResult()
        {
            int roomId = 1;
            var mealPlans = new List<RoomMealPlan>
            {
                new RoomMealPlan
                {
                    Id = 1,
                    RoomId = roomId,
                    Recipe = new Recipe
                    {
                        Id = 10,
                        Ingredients = new List<Ingredient>
                        {
                            new Ingredient { Name = "Молоко", Amount = "500 мл" },
                            new Ingredient { Name = "Цукор", Amount = "50 г" }
                        }
                    }
                }
            };

            _mealPlanRepoMock.Setup(r => r.GetByRoomIdWithRecipesAsync(roomId))
                .ReturnsAsync(mealPlans);

            var result = await _service.GenerateShoppingListFromMealPlanAsync(roomId);

            Assert.True(result.IsSuccess);
            _shoppingRepoMock.Verify(r => r.ClearRoomListAsync(roomId), Times.Once);
            _shoppingRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<ShoppingListItem>>()), Times.Once);
        }

        [Fact]
        public async Task GenerateShoppingList_WhenRoomIdIsInvalid_ReturnsFailureResult()
        {
            var result = await _service.GenerateShoppingListFromMealPlanAsync(0);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор кімнати.", result.Error);
            _shoppingRepoMock.Verify(r => r.ClearRoomListAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GenerateShoppingList_WhenMealPlanIsEmpty_ReturnsSuccessWithWarningMessage()
        {
            int roomId = 1;
            _mealPlanRepoMock.Setup(r => r.GetByRoomIdWithRecipesAsync(roomId))
                .ReturnsAsync(new List<RoomMealPlan>());

            var result = await _service.GenerateShoppingListFromMealPlanAsync(roomId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Спільне меню порожнє. Немає страв для генерації списку.", result.Value);
            _shoppingRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<ShoppingListItem>>()), Times.Never);
        }

        [Fact]
        public async Task GenerateShoppingList_WithManualPlans_ReturnsSuccessWithWarningAboutManualDishes()
        {
            int roomId = 1;
            var mealPlans = new List<RoomMealPlan>
            {
                new RoomMealPlan
                {
                    Id = 1,
                    RoomId = roomId,
                    RecipeName = "Борщ",
                    RecipeId = null,
                    Recipe = null
                }
            };

            _mealPlanRepoMock.Setup(r => r.GetByRoomIdWithRecipesAsync(roomId))
                .ReturnsAsync(mealPlans);

            var result = await _service.GenerateShoppingListFromMealPlanAsync(roomId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Contains("У вашому меню є страви, додані вручну", result.Value);
        }

        [Fact]
        public async Task GenerateShoppingList_WhenRepositoryReturnsNull_ReturnsSuccessWithEmptyMessage()
        {
            int roomId = 1;
            _mealPlanRepoMock.Setup(r => r.GetByRoomIdWithRecipesAsync(roomId))
                .ReturnsAsync((List<RoomMealPlan>)null!);

            var result = await _service.GenerateShoppingListFromMealPlanAsync(roomId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Спільне меню порожнє. Немає страв для генерації списку.", result.Value);
            _shoppingRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<List<ShoppingListItem>>()), Times.Never);
        }
    }
}