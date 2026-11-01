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
    public class FridgeServiceTests
    {
        private readonly Mock<IFridgeRepository> _repoMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly FridgeService _service;

        public FridgeServiceTests()
        {
            _repoMock = new Mock<IFridgeRepository>();
            _loggerMock = new Mock<ILogger>();
            _service = new FridgeService(_repoMock.Object, _loggerMock.Object);
        }

        #region AddItemAsync Tests (Холодильник - Додавання)

        [Fact]
        public async Task AddItemAsync_WithValidRequest_ReturnsSuccessResult()
        {
            var request = new AddFridgeItemRequest
            {
                UserId = 1,
                IngredientName = "Молоко",
                Quantity = 1,
                Unit = "л"
            };

            _repoMock.Setup(r => r.ExistsAsync(request.UserId, "Молоко"))
                .ReturnsAsync(false);

            var result = await _service.AddItemAsync(request);

            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserFridgeItem>()), Times.Once);
        }

        [Fact]
        public async Task AddItemAsync_WhenRequestIsNull_ReturnsFailureResult()
        {
            var result = await _service.AddItemAsync(null!);

            Assert.True(result.IsFailure);
            Assert.Equal("Дані продукту не задано.", result.Error);
        }

        [Fact]
        public async Task AddItemAsync_WhenIngredientNameIsEmpty_ReturnsFailureResult()
        {
            var request = new AddFridgeItemRequest
            {
                UserId = 1,
                IngredientName = "   ",
                Quantity = 2,
                Unit = "кг"
            };

            var result = await _service.AddItemAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва продукту не може бути порожньою.", result.Error);
        }

        [Fact]
        public async Task AddItemAsync_WhenItemAlreadyExists_ReturnsFailureResult()
        {
            var request = new AddFridgeItemRequest
            {
                UserId = 1,
                IngredientName = "Яблука",
                Quantity = 5,
                Unit = "шт"
            };

            _repoMock.Setup(r => r.ExistsAsync(request.UserId, "Яблука"))
                .ReturnsAsync(true);

            var result = await _service.AddItemAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Цей продукт вже є у вашому холодильнику.", result.Error);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<UserFridgeItem>()), Times.Never);
        }

        #endregion

        #region RemoveItemAsync Tests (Холодильник - Видалення)

        [Fact]
        public async Task RemoveItemAsync_WithValidItem_ReturnsSuccessResult()
        {
            var request = new RemoveFridgeItemRequest
            {
                UserId = 1,
                ItemId = 10
            };

            var existingItem = new UserFridgeItem
            {
                Id = 10,
                UserId = 1,
                IngredientName = "Сир"
            };

            _repoMock.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(existingItem);

            var result = await _service.RemoveItemAsync(request);

            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.RemoveAsync(10), Times.Once);
        }

        [Fact]
        public async Task RemoveItemAsync_WhenItemDoesNotBelongToUser_ReturnsFailureResult()
        {
            var request = new RemoveFridgeItemRequest
            {
                UserId = 1,
                ItemId = 10
            };

            var existingItem = new UserFridgeItem
            {
                Id = 10,
                UserId = 2,
                IngredientName = "Сир"
            };

            _repoMock.Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(existingItem);

            var result = await _service.RemoveItemAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Продукт не знайдено.", result.Error);
            _repoMock.Verify(r => r.RemoveAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region GetUserFridgeAsync Tests (Холодильник - Отримання списку)

        [Fact]
        public async Task GetUserFridgeAsync_WithValidUserId_ReturnsItemsList()
        {
            int userId = 1;
            var expectedItems = new List<UserFridgeItem>
            {
                new() { Id = 1, UserId = userId, IngredientName = "Вода", Quantity = 2, Unit = "л" }
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(expectedItems);

            var result = await _service.GetUserFridgeAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("Вода", result.Value[0].IngredientName);
        }

        [Fact]
        public async Task GetUserFridgeAsync_WithInvalidUserId_ReturnsFailureResult()
        {
            var result = await _service.GetUserFridgeAsync(0);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор користувача.", result.Error);
        }

        #endregion

        #region AddShoppingItemAsync Tests (Список покупок - Додавання)

        [Fact]
        public async Task AddShoppingItemAsync_WithValidRequest_ReturnsSuccessResult()
        {
            var request = new AddShoppingItemRequest
            {
                UserId = 1,
                IngredientName = "Хліб",
                Quantity = 1,
                Unit = "шт"
            };

            var result = await _service.AddShoppingItemAsync(request);

            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.AddShoppingItemAsync(It.IsAny<UserShoppingItem>()), Times.Once);
        }

        [Fact]
        public async Task AddShoppingItemAsync_WhenNameIsEmpty_ReturnsFailureResult()
        {
            var request = new AddShoppingItemRequest
            {
                UserId = 1,
                IngredientName = "",
                Quantity = 1
            };

            var result = await _service.AddShoppingItemAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва продукту не може бути порожньою.", result.Error);
        }

        #endregion

        #region RemoveShoppingItemAsync Tests (Список покупок - Видалення)

        [Fact]
        public async Task RemoveShoppingItemAsync_WithValidItem_ReturnsSuccessResult()
        {
            var request = new RemoveShoppingItemRequest
            {
                UserId = 1,
                ItemId = 5
            };

            var shoppingItem = new UserShoppingItem
            {
                Id = 5,
                UserId = 1,
                IngredientName = "Цукор"
            };

            _repoMock.Setup(r => r.GetShoppingItemByIdAsync(5))
                .ReturnsAsync(shoppingItem);

            var result = await _service.RemoveShoppingItemAsync(request);

            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.RemoveShoppingItemAsync(5), Times.Once);
        }

        #endregion
    }
}