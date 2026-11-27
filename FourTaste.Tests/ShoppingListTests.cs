using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using FourTaste.WPF.ViewModels;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class ShoppingListTests
    {
        private readonly Mock<IShoppingListRepository> _shoppingListRepositoryMock;
        private readonly Mock<IRoomMealPlanRepository> _mealPlanRepositoryMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ShoppingListService _shoppingListService;

        public ShoppingListTests()
        {
            _shoppingListRepositoryMock = new Mock<IShoppingListRepository>();
            _mealPlanRepositoryMock = new Mock<IRoomMealPlanRepository>();
            _loggerMock = new Mock<ILogger>();

            _shoppingListService = new ShoppingListService(
                _shoppingListRepositoryMock.Object,
                _mealPlanRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task AddManualItemAsync_ValidInput_ShouldFormatNameAndSaveItem()
        {
            int roomId = 4;
            string rawName = "  картопля  ";
            decimal quantity = 2.5m;
            string unit = " кг ";

            _shoppingListRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<ShoppingListItem>()))
                .Returns(Task.CompletedTask);

            var result = await _shoppingListService.AddManualItemAsync(roomId, rawName, quantity, unit);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(roomId, result.Value.RoomId);
            Assert.Equal("Картопля", result.Value.ProductName);
            Assert.Equal(2.5m, result.Value.Quantity);
            Assert.Equal("кг", result.Value.Unit);
            Assert.False(result.Value.IsChecked);

            _shoppingListRepositoryMock.Verify(repo => repo.AddAsync(It.Is<ShoppingListItem>(i =>
                i.RoomId == roomId &&
                i.ProductName == "Картопля" &&
                i.Quantity == 2.5m &&
                i.Unit == "кг")), Times.Once);
        }

        [Fact]
        public async Task AddManualItemAsync_EmptyProductName_ShouldReturnFailureResult()
        {
            int roomId = 4;

            var result = await _shoppingListService.AddManualItemAsync(roomId, "   ", 1m, "шт");

            Assert.True(result.IsFailure);
            Assert.Equal("Введіть назву продукту.", result.Error);
            _shoppingListRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ShoppingListItem>()), Times.Never);
        }

        [Fact]
        public async Task AddManualItemAsync_InvalidRoomId_ShouldReturnFailureResult()
        {
            var result = await _shoppingListService.AddManualItemAsync(0, "Молоко", 1m, "л");

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор кімнати.", result.Error);
            _shoppingListRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<ShoppingListItem>()), Times.Never);
        }

        [Fact]
        public async Task RemoveItemAsync_ValidIds_ShouldDeleteFromRepositoryAndReturnSuccess()
        {
            int roomId = 4;
            int itemId = 414;

            _shoppingListRepositoryMock
                .Setup(repo => repo.DeleteAsync(roomId, itemId))
                .Returns(Task.CompletedTask);

            var result = await _shoppingListService.RemoveItemAsync(roomId, itemId);

            Assert.True(result.IsSuccess);
            _shoppingListRepositoryMock.Verify(repo => repo.DeleteAsync(roomId, itemId), Times.Once);
        }

        [Fact]
        public async Task RemoveItemAsync_InvalidItemId_ShouldReturnFailureResult()
        {
            var result = await _shoppingListService.RemoveItemAsync(4, 0);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор продукту або кімнати.", result.Error);
            _shoppingListRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ClearListAsync_ValidRoomId_ShouldClearRoomListInRepository()
        {
            int roomId = 4;

            _shoppingListRepositoryMock
                .Setup(repo => repo.ClearRoomListAsync(roomId))
                .Returns(Task.CompletedTask);

            var result = await _shoppingListService.ClearListAsync(roomId);

            Assert.True(result.IsSuccess);
            _shoppingListRepositoryMock.Verify(repo => repo.ClearRoomListAsync(roomId), Times.Once);
        }

        [Fact]
        public async Task GenerateShoppingListFromMealPlanAsync_EmptyMealPlans_ShouldReturnInformationalMessage()
        {
            int roomId = 4;

            _shoppingListRepositoryMock
                .Setup(repo => repo.ClearRoomListAsync(roomId))
                .Returns(Task.CompletedTask);

            _mealPlanRepositoryMock
                .Setup(repo => repo.GetByRoomIdWithRecipesAsync(roomId))
                .ReturnsAsync(new List<RoomMealPlan>());

            var result = await _shoppingListService.GenerateShoppingListFromMealPlanAsync(roomId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Спільне меню порожнє. Немає страв для генерації списку.", result.Value);
            _shoppingListRepositoryMock.Verify(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<ShoppingListItem>>()), Times.Never);
        }

        [Fact]
        public async Task ShoppingListViewModel_AddManualItemFromInputAsync_ShouldAddItemToCollection()
        {
            int roomId = 4;
            _shoppingListRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<ShoppingListItem>()))
                .Returns(Task.CompletedTask);

            var viewModel = new ShoppingListViewModel(roomId, _shoppingListService);

            await viewModel.AddManualItemFromInputAsync("Яблука", "3", "шт");

            Assert.Single(viewModel.Items);
            Assert.Equal("Яблука", viewModel.Items[0].ProductName);
            Assert.Equal(3m, viewModel.Items[0].Quantity);
            Assert.Equal("шт", viewModel.Items[0].Unit);
            Assert.False(viewModel.IsEmpty);
        }

        [Fact]
        public async Task ShoppingListViewModel_RemoveSelectedItemAsync_ShouldRemoveSelectedItemFromCollection()
        {
            int roomId = 4;
            var item = new ShoppingListItem
            {
                Id = 10,
                RoomId = roomId,
                ProductName = "Хліб",
                Quantity = 1m,
                Unit = "шт"
            };

            _shoppingListRepositoryMock
                .Setup(repo => repo.GetByRoomIdAsync(roomId))
                .ReturnsAsync(new List<ShoppingListItem> { item });

            _shoppingListRepositoryMock
                .Setup(repo => repo.DeleteAsync(roomId, item.Id))
                .Returns(Task.CompletedTask);

            var viewModel = new ShoppingListViewModel(roomId, _shoppingListService);
            await viewModel.LoadAsync();

            viewModel.SelectedItem = item;
            Assert.True(viewModel.RemoveSelectedItemCommand.CanExecute(null));

            await viewModel.RemoveSelectedItemAsync();

            Assert.Empty(viewModel.Items);
            Assert.Null(viewModel.SelectedItem);
            Assert.True(viewModel.IsEmpty);
            _shoppingListRepositoryMock.Verify(repo => repo.DeleteAsync(roomId, item.Id), Times.Once);
        }
    }
}
