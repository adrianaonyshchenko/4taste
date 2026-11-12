using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class ShoppingListService
    {
        private readonly IShoppingListRepository _shoppingListRepository;
        private readonly IRoomMealPlanRepository _mealPlanRepository;
        private readonly ILogger _logger;

        public ShoppingListService(
            IShoppingListRepository shoppingListRepository,
            IRoomMealPlanRepository mealPlanRepository,
            ILogger logger)
        {
            _shoppingListRepository = shoppingListRepository;
            _mealPlanRepository = mealPlanRepository;
            _logger = logger;
        }

        public async Task<Result<string?>> GenerateShoppingListFromMealPlanAsync(int roomId)
        {
            if (roomId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний RoomId ({RoomId}) при генерації списку покупок.", roomId);
                return Result<string?>.Failure("Некоректний ідентифікатор кімнати.");
            }

            _logger.Information("Початок генерації списку покупок для кімнати {RoomId}.", roomId);

            await _shoppingListRepository.ClearRoomListAsync(roomId);

            var mealPlans = await _mealPlanRepository.GetByRoomIdWithRecipesAsync(roomId);
            if (mealPlans == null || mealPlans.Count == 0)
            {
                return Result<string?>.Success("Спільне меню порожнє. Немає страв для генерації списку.");
            }

            var manualPlans = mealPlans.Where(m => !m.RecipeId.HasValue || m.Recipe == null).ToList();
            string? warningMessage = null;

            if (manualPlans.Count > 0)
            {
                var manualNames = string.Join(", ", manualPlans.Select(m => $"«{m.RecipeName}»"));
                warningMessage = $"У вашому меню є страви, додані вручну ({manualNames}). Їхні інгредієнти не додано до списку автоматично — ви можете додати потрібні продукти вручну нижче.";
            }

            var aggregatedIngredients = new Dictionary<(string Name, string Unit), decimal>();

            foreach (var plan in mealPlans)
            {
                if (plan.Recipe?.Ingredients != null)
                {
                    foreach (var ingredient in plan.Recipe.Ingredients)
                    {
                        if (string.IsNullOrWhiteSpace(ingredient.Name))
                        {
                            continue;
                        }

                        string productName = ingredient.Name.Trim();
                        decimal parsedQuantity = 1;
                        string unitName = string.Empty;

                        if (!string.IsNullOrWhiteSpace(ingredient.Amount))
                        {
                            var amountStr = ingredient.Amount.Trim();

                            var match = Regex.Match(amountStr, @"^([\d\.,]+)");
                            if (match.Success)
                            {
                                var numPart = match.Groups[1].Value.Replace(',', '.');
                                if (decimal.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal q))
                                {
                                    parsedQuantity = q;
                                }
                                unitName = amountStr.Substring(match.Length).Trim();
                            }
                            else
                            {
                                unitName = amountStr;
                            }
                        }

                        string normalizedName = productName.ToLowerInvariant();
                        string normalizedUnit = unitName.ToLowerInvariant();

                        var key = (Name: normalizedName, Unit: normalizedUnit);

                        if (aggregatedIngredients.ContainsKey(key))
                        {
                            aggregatedIngredients[key] += parsedQuantity;
                        }
                        else
                        {
                            aggregatedIngredients[key] = parsedQuantity;
                        }
                    }
                }
            }

            var newItems = new List<ShoppingListItem>();
            foreach (var kvp in aggregatedIngredients)
            {
                string formattedName = char.ToUpper(kvp.Key.Name[0]) + (kvp.Key.Name.Length > 1 ? kvp.Key.Name.Substring(1) : string.Empty);

                newItems.Add(new ShoppingListItem
                {
                    RoomId = roomId,
                    ProductName = formattedName,
                    Quantity = kvp.Value,
                    Unit = kvp.Key.Unit,
                    IsChecked = false
                });
            }

            if (newItems.Count > 0)
            {
                await _shoppingListRepository.AddRangeAsync(newItems);
            }

            _logger.Information("Список покупок для кімнати {RoomId} успішно згенеровано. Додано {Count} позицій.", roomId, newItems.Count);

            return Result<string?>.Success(warningMessage);
        }
    }
}