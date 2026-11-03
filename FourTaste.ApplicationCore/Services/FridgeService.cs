using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class FridgeService : IFridgeService
    {
        private readonly IFridgeRepository _fridgeRepository;
        private readonly ILogger _logger;

        public FridgeService(IFridgeRepository fridgeRepository, ILogger logger)
        {
            _fridgeRepository = fridgeRepository;
            _logger = logger;
        }

        public async Task<Result<int>> AddItemAsync(AddFridgeItemRequest request)
        {
            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на додавання продукту в холодильник порожній.");
                return Result<int>.Failure("Дані продукту не задано.");
            }

            if (string.IsNullOrWhiteSpace(request.IngredientName))
            {
                _logger.Warning("Помилка валідації: назва продукту порожня.");
                return Result<int>.Failure("Назва продукту не може бути порожньою.");
            }

            var trimmedName = request.IngredientName.Trim();
            var unit = string.IsNullOrWhiteSpace(request.Unit) ? "шт" : request.Unit.Trim();
            _logger.Information("Спроба додати продукт '{Ingredient}' (кількість: {Quantity} {Unit}) до холодильника користувача {UserId}", trimmedName, request.Quantity, unit, request.UserId);

            bool exists = await _fridgeRepository.ExistsAsync(request.UserId, trimmedName);
            if (exists)
            {
                _logger.Warning("Продукт '{Ingredient}' вже є в холодильнику користувача {UserId}", trimmedName, request.UserId);
                return Result<int>.Failure("Цей продукт вже є у вашому холодильнику.");
            }

            var fridgeItem = new UserFridgeItem
            {
                UserId = request.UserId,
                IngredientName = trimmedName,
                Quantity = request.Quantity > 0 ? request.Quantity : 1,
                Unit = unit
            };

            await _fridgeRepository.AddAsync(fridgeItem);
            _logger.Information("Продукт '{Ingredient}' (Id {ItemId}) успішно додано до холодильника користувача {UserId}", trimmedName, fridgeItem.Id, request.UserId);

            return Result<int>.Success(fridgeItem.Id);
        }

        public async Task<Result> RemoveItemAsync(RemoveFridgeItemRequest request)
        {
            _logger.Information("Спроба видалити продукт з Id {ItemId} з холодильника користувача {UserId}", request.ItemId, request.UserId);

            var item = await _fridgeRepository.GetByIdAsync(request.ItemId);
            if (item == null || item.UserId != request.UserId)
            {
                _logger.Warning("Продукт з Id {ItemId} не знайдено або він не належить користувачу {UserId}", request.ItemId, request.UserId);
                return Result.Failure("Продукт не знайдено.");
            }

            await _fridgeRepository.RemoveAsync(request.ItemId);
            _logger.Information("Продукт з Id {ItemId} успішно видалено з холодильника", request.ItemId);

            return Result.Success();
        }

        public async Task<Result<List<UserFridgeItem>>> GetUserFridgeAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба отримати холодильник для некоректного UserId {UserId}", userId);
                return Result<List<UserFridgeItem>>.Failure("Некоректний ідентифікатор користувача.");
            }

            _logger.Information("Отримання списку продуктів у холодильнику для користувача {UserId}", userId);
            var items = await _fridgeRepository.GetByUserIdAsync(userId);

            return Result<List<UserFridgeItem>>.Success(items);
        }
  

        public async Task<Result<int>> AddShoppingItemAsync(AddShoppingItemRequest request)
        {
            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на додавання продукту до списку покупок порожній.");
                return Result<int>.Failure("Дані продукту не задано.");
            }

            if (string.IsNullOrWhiteSpace(request.IngredientName))
            {
                _logger.Warning("Помилка валідації: назва продукту для покупки порожня.");
                return Result<int>.Failure("Назва продукту не може бути порожньою.");
            }

            var trimmedName = request.IngredientName.Trim();
            var unit = string.IsNullOrWhiteSpace(request.Unit) ? "шт" : request.Unit.Trim();
            _logger.Information("Спроба додати продукт '{Ingredient}' (кількість: {Quantity} {Unit}) до списку покупок користувача {UserId}", trimmedName, request.Quantity, unit, request.UserId);

            var shoppingItem = new UserShoppingItem
            {
                UserId = request.UserId,
                IngredientName = trimmedName,
                Quantity = request.Quantity > 0 ? request.Quantity : 1,
                Unit = unit
            };

            await _fridgeRepository.AddShoppingItemAsync(shoppingItem);
            _logger.Information("Продукт '{Ingredient}' (Id {ItemId}) успішно додано до списку покупок користувача {UserId}", trimmedName, shoppingItem.Id, request.UserId);

            return Result<int>.Success(shoppingItem.Id);
        }

        public async Task<Result> RemoveShoppingItemAsync(RemoveShoppingItemRequest request)
        {
            _logger.Information("Спроба видалити продукт з Id {ItemId} зі списку покупок користувача {UserId}", request.ItemId, request.UserId);

            var item = await _fridgeRepository.GetShoppingItemByIdAsync(request.ItemId);
            if (item == null || item.UserId != request.UserId)
            {
                _logger.Warning("Продукт з Id {ItemId} у списку покупок не знайдено або він не належить користувачу {UserId}", request.ItemId, request.UserId);
                return Result.Failure("Продукт не знайдено.");
            }

            await _fridgeRepository.RemoveShoppingItemAsync(request.ItemId);
            _logger.Information("Продукт з Id {ItemId} успішно видалено зі списку покупок", request.ItemId);

            return Result.Success();
        }

        public async Task<Result<List<UserShoppingItem>>> GetShoppingListAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба отримати список покупок для некоректного UserId {UserId}", userId);
                return Result<List<UserShoppingItem>>.Failure("Некоректний ідентифікатор користувача.");
            }

            _logger.Information("Отримання списку покупок для користувача {UserId}", userId);
            var items = await _fridgeRepository.GetShoppingItemsByUserIdAsync(userId);

            return Result<List<UserShoppingItem>>.Success(items);
        }
    }
}