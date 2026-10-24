using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Microsoft.Extensions.Logging;

namespace FourTaste.ApplicationCore.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly ILogger<RecipeService> _logger;

        public RecipeService(IRecipeRepository recipeRepository, ILogger<RecipeService> logger)
        {
            _recipeRepository = recipeRepository;
            _logger = logger;
        }

        public async Task<Result> CreateRecipeAsync(string title, string time, string category, string cuisine, bool isPublic, int userId, List<string> ingredients, List<string> steps)
        {
            _logger.LogInformation("Спроба створення нового рецепта: {Title}", title);

            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Помилка валідації: Назва рецепта порожня.");
                return Result.Failure("Назва рецепта не може бути порожньою.");
            }

            if (ingredients == null || ingredients.Count == 0)
            {
                _logger.LogWarning("Помилка валідації: Список інгредієнтів порожній.");
                return Result.Failure("Додайте хоча б один інгредієнт.");
            }

            if (steps == null || steps.Count == 0)
            {
                _logger.LogWarning("Помилка валідації: Список кроків порожній.");
                return Result.Failure("Додайте кроки приготування.");
            }

            try
            {
                var recipe = new Recipe
                {
                    Title = title.Trim(),
                    Time = time,
                    Category = category,
                    Cuisine = cuisine,
                    IsPublic = isPublic,
                    UserId = userId
                };

                foreach (var ingName in ingredients)
                {
                    recipe.Ingredients.Add(new Ingredient { Name = ingName });
                }

                int stepNum = 1;
                foreach (var stepDesc in steps)
                {
                    recipe.Steps.Add(new RecipeStep { StepNumber = stepNum++, Description = stepDesc });
                }

                await _recipeRepository.AddAsync(recipe);

                _logger.LogInformation("Рецепт '{Title}' успішно створено.", title);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час збереження рецепта в базу даних.");
                return Result.Failure("Сталася системна помилка при збереженні.");
            }
        }
    }
}