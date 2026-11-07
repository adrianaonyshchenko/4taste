using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class RecipeService : IRecipeService
    {
        private const int MaxPhotoSizeBytes = 5 * 1024 * 1024;

        private readonly IRecipeRepository _recipeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFridgeRepository _fridgeRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IAllergyService _allergyService;
        private readonly ILogger _logger;

        public RecipeService(
            IRecipeRepository recipeRepository,
            IUserRepository userRepository,
            IFridgeRepository fridgeRepository,
            IFavoriteRepository favoriteRepository,
            IAllergyService allergyService,
            ILogger logger)
        {
            _recipeRepository = recipeRepository;
            _userRepository = userRepository;
            _fridgeRepository = fridgeRepository;
            _favoriteRepository = favoriteRepository;
            _allergyService = allergyService;
            _logger = logger;
        }

        public IReadOnlyList<string> GetAvailableCuisines()
        {
            return RecipeOptions.Cuisines;
        }

        public IReadOnlyList<string> GetAvailableCategories()
        {
            return RecipeOptions.Categories;
        }

        public async Task<Result<int>> CreateRecipeAsync(CreateRecipeRequest request)
        {
            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на створення рецепта порожній.");
                return Result<int>.Failure("Дані рецепта не задано.");
            }

            _logger.Information("Спроба створення нового рецепта: {Title}", request.Title);

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.Warning("Помилка валідації: назва рецепта порожня.");
                return Result<int>.Failure("Назва рецепта не може бути порожньою.");
            }

            if (request.Photo != null && request.Photo.Length > MaxPhotoSizeBytes)
            {
                _logger.Warning("Помилка валідації: розмір фото перевищує 5 МБ.");
                return Result<int>.Failure("Розмір фотографії не може перевищувати 5 МБ.");
            }

            bool hasIngredients = (request.Ingredients != null && request.Ingredients.Count > 0) ||
                                  (request.IngredientItems != null && request.IngredientItems.Count > 0);
            if (!hasIngredients)
            {
                _logger.Warning("Помилка валідації: список інгредієнтів порожній.");
                return Result<int>.Failure("Додайте хоча б один інгредієнт.");
            }

            if (request.Steps == null || request.Steps.Count == 0)
            {
                _logger.Warning("Помилка валідації: список кроків порожній.");
                return Result<int>.Failure("Додайте кроки приготування.");
            }

            var recipe = new Recipe
            {
                Title = request.Title.Trim(),
                Time = request.Time,
                Category = request.Category,
                Cuisine = request.Cuisine,
                IsPublic = request.IsPublic,
                UserId = request.UserId,
                Photo = request.Photo,
            };

            if (request.IngredientItems != null && request.IngredientItems.Count > 0)
            {
                foreach (var ing in request.IngredientItems.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
                {
                    recipe.Ingredients.Add(new Ingredient
                    {
                        Name = ing.Name.Trim(),
                        Amount = string.IsNullOrWhiteSpace(ing.Amount) ? "1 шт." : ing.Amount.Trim(),
                    });
                }
            }
            else if (request.Ingredients != null)
            {
                foreach (var ingName in request.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i)))
                {
                    recipe.Ingredients.Add(new Ingredient { Name = ingName.Trim(), Amount = "1 шт." });
                }
            }

            int stepNum = 1;
            foreach (var stepDesc in request.Steps)
            {
                recipe.Steps.Add(new RecipeStep { StepNumber = stepNum++, Description = stepDesc });
            }

            await _recipeRepository.AddAsync(recipe);

            _logger.Information("Рецепт '{Title}' (Id {RecipeId}) успішно створено.", recipe.Title, recipe.Id);
            return Result<int>.Success(recipe.Id);
        }

        public async Task<Result> UpdateRecipeAsync(UpdateRecipeRequest request)
        {
            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на оновлення рецепта порожній.");
                return Result.Failure("Дані рецепта не задано.");
            }

            if (request.Id <= 0 || request.UserId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний Id ({RecipeId}) або UserId ({UserId}).", request.Id, request.UserId);
                return Result.Failure("Некоректні дані запиту.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(request.Id);
            if (recipe is null)
            {
                _logger.Warning("Оновлення: рецепт {RecipeId} не знайдено.", request.Id);
                return Result.Failure("Рецепт не знайдено.");
            }

            // Перевірка прав доступу: лише автор може редагувати рецепт
            if (recipe.UserId != request.UserId)
            {
                _logger.Warning("Спроба несанкціонованого редагування: користувач {UserId} намагався змінити рецепт {RecipeId} автора {AuthorId}.", request.UserId, request.Id, recipe.UserId);
                return Result.Failure("Ви можете редагувати лише власні рецепти.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.Warning("Помилка валідації: назва рецепта порожня.");
                return Result.Failure("Назва рецепта не може бути порожньою.");
            }

            if (string.IsNullOrWhiteSpace(request.Category))
            {
                _logger.Warning("Помилка валідації: категорія не вибрана.");
                return Result.Failure("Оберіть категорію рецепта.");
            }

            if (string.IsNullOrWhiteSpace(request.Cuisine))
            {
                _logger.Warning("Помилка валідації: країна/кухня не вибрана.");
                return Result.Failure("Оберіть країну/кухню рецепта.");
            }

            if (!IsValidCookingTime(request.Time))
            {
                _logger.Warning("Помилка валідації: некоректний час приготування '{Time}'.", request.Time);
                return Result.Failure("Час приготування має бути у форматі 00:00 і бути більшим за нуль.");
            }

            if (request.Servings <= 0)
            {
                _logger.Warning("Помилка валідації: кількість порцій {Servings} <= 0.", request.Servings);
                return Result.Failure("Кількість порцій повинна бути більшою за нуль.");
            }

            if (request.Photo != null && request.Photo.Length > MaxPhotoSizeBytes)
            {
                _logger.Warning("Помилка валідації: розмір фото перевищує 5 МБ.");
                return Result.Failure("Розмір фотографії не може перевищувати 5 МБ.");
            }

            var validIngredients = request.Ingredients?
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .ToList() ?? new List<RecipeIngredientDto>();

            if (validIngredients.Count == 0)
            {
                _logger.Warning("Помилка валідації: список інгредієнтів порожній.");
                return Result.Failure("Додайте хоча б один інгредієнт.");
            }

            if (validIngredients.Any(i => string.IsNullOrWhiteSpace(i.Amount)))
            {
                _logger.Warning("Помилка валідації: не для всіх інгредієнтів вказано кількість.");
                return Result.Failure("Вкажіть кількість для кожного інгредієнта.");
            }

            var validSteps = request.Steps?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            if (validSteps.Count == 0)
            {
                _logger.Warning("Помилка валідації: список кроків порожній.");
                return Result.Failure("Додайте кроки приготування.");
            }

            recipe.Title = request.Title.Trim();
            recipe.Time = request.Time.Trim();
            recipe.Category = request.Category.Trim();
            recipe.Cuisine = request.Cuisine.Trim();
            recipe.IsPublic = request.IsPublic;
            recipe.Servings = request.Servings;

            if (request.UpdatePhoto)
            {
                recipe.Photo = request.Photo;
            }

            recipe.Ingredients = validIngredients.Select(i => new Ingredient
            {
                Name = i.Name.Trim(),
                Amount = i.Amount.Trim(),
                RecipeId = recipe.Id,
            }).ToList();

            int stepNum = 1;
            recipe.Steps = validSteps.Select(s => new RecipeStep
            {
                StepNumber = stepNum++,
                Description = s,
                RecipeId = recipe.Id,
            }).ToList();

            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("Рецепт '{Title}' (Id {RecipeId}) успішно оновлено користувачем {UserId}.", recipe.Title, recipe.Id, request.UserId);
            return Result.Success();
        }

        public async Task<Result> DeleteRecipeAsync(int recipeId, int userId)
        {
            if (recipeId <= 0 || userId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректні дані для видалення рецепта (Id: {RecipeId}, UserId: {UserId}).", recipeId, userId);
                return Result.Failure("Некоректні дані для видалення рецепта.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe is null)
            {
                _logger.Warning("Видалення: рецепт {RecipeId} не знайдено.", recipeId);
                return Result.Failure("Рецепт не знайдено.");
            }

            // Перевірка прав доступу: лише автор може видалити рецепт
            if (recipe.UserId != userId)
            {
                _logger.Warning("Спроба несанкціонованого видалення: користувач {UserId} намагався видалити рецепт {RecipeId} автора {AuthorId}.", userId, recipeId, recipe.UserId);
                return Result.Failure("Ви можете видаляти лише власні рецепти.");
            }

            await _recipeRepository.DeleteAsync(recipeId);
            _logger.Information("Рецепт {RecipeId} успішно видалено користувачем {UserId}.", recipeId, userId);
            return Result.Success();
        }

        public async Task<Result<List<Recipe>>> SearchRecipesAsync(RecipeSearchCriteria criteria)
        {
            if (criteria is null)
            {
                _logger.Warning("Помилка валідації: критерії пошуку не задано.");
                return Result<List<Recipe>>.Failure("Критерії пошуку не задано.");
            }

            _logger.Information(
                "Пошук рецептів: запит '{SearchTerm}', фільтр {Filter}, лише публічні: {OnlyPublic}, власник: {OwnerUserId}",
                criteria.SearchTerm,
                criteria.Filter,
                criteria.OnlyPublic,
                criteria.OwnerUserId);

            var recipes = await _recipeRepository.SearchAsync(criteria);

            _logger.Information("Знайдено {Count} рецептів", recipes.Count);
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<List<Recipe>>> FindRecipesByProductsAsync(FindRecipesByProductsRequest request)
        {
            if (request is null || request.UserId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний запит на пошук рецептів за продуктами.");
                return Result<List<Recipe>>.Failure("Некоректні дані запиту.");
            }

            var fridgeItems = await _fridgeRepository.GetByUserIdAsync(request.UserId);
            if (fridgeItems.Count == 0)
            {
                _logger.Information("Пошук за продуктами: у користувача {UserId} порожній список продуктів.", request.UserId);
                return Result<List<Recipe>>.Failure("Додайте продукти до списку «У мене є», щоб знайти рецепти.");
            }

            var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(request.UserId);
            if (allergiesResult.IsFailure)
            {
                _logger.Warning("Пошук за продуктами: не вдалося отримати алергени користувача {UserId}.", request.UserId);
                return Result<List<Recipe>>.Failure("Не вдалося отримати ваші алергени. Спробуйте ще раз.");
            }

            var criteria = new RecipeSearchCriteria
            {
                ViewerUserId = request.UserId,
                AvailableProducts = fridgeItems.Select(i => i.IngredientName).ToList(),
                ExcludedAllergens = allergiesResult.Value!.Select(a => a.AllergenName).ToList(),
                Category = request.Category,
                Cuisine = request.Cuisine,
                MaxCookingMinutes = request.MaxCookingMinutes,
            };

            _logger.Information(
                "Пошук за продуктами для користувача {UserId}: продуктів {Products}, алергенів {Allergens}",
                request.UserId,
                criteria.AvailableProducts.Count,
                criteria.ExcludedAllergens.Count);

            var recipes = await _recipeRepository.SearchAsync(criteria);

            _logger.Information("Пошук за продуктами: знайдено {Count} рецептів", recipes.Count);
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<RecipeDetails>> GetRecipeDetailsAsync(int recipeId, int? viewerUserId)
        {
            if (recipeId <= 0)
            {
                _logger.Warning("Спроба переглянути рецепт з некоректним Id {RecipeId}", recipeId);
                return Result<RecipeDetails>.Failure("Некоректний ідентифікатор рецепта.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);

            if (recipe is null)
            {
                _logger.Warning("Рецепт з Id {RecipeId} не знайдено", recipeId);
                return Result<RecipeDetails>.Failure("Рецепт не знайдено.");
            }

            if (!recipe.IsPublic && recipe.UserId != viewerUserId)
            {
                _logger.Warning("Спроба перегляду приватного рецепта {RecipeId} без доступу (глядач: {ViewerUserId})", recipeId, viewerUserId);
                return Result<RecipeDetails>.Failure("Цей рецепт приватний.");
            }

            var author = await _userRepository.GetByIdAsync(recipe.UserId);

            _logger.Information("Рецепт {RecipeId} переглянуто (глядач: {ViewerUserId})", recipeId, viewerUserId);

            return Result<RecipeDetails>.Success(new RecipeDetails
            {
                Recipe = recipe,
                AuthorName = author?.Username ?? "Невідомий автор",
            });
        }

        public async Task<Result> ToggleFavoriteAsync(int userId, int recipeId)
        {
            if (userId <= 0 || recipeId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний UserId ({UserId}) або RecipeId ({RecipeId}) при перемиканні обраного.", userId, recipeId);
                return Result.Failure("Некоректні дані запиту.");
            }

            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe is null)
            {
                _logger.Warning("Обране: рецепт з Id {RecipeId} не знайдено.", recipeId);
                return Result.Failure("Рецепт не знайдено.");
            }

            bool isFav = await _favoriteRepository.IsFavoriteAsync(userId, recipeId);
            if (isFav)
            {
                await _favoriteRepository.RemoveFavoriteAsync(userId, recipeId);
                _logger.Information("Користувач {UserId} видалив рецепт {RecipeId} з обраного.", userId, recipeId);
            }
            else
            {
                await _favoriteRepository.AddFavoriteAsync(userId, recipeId);
                _logger.Information("Користувач {UserId} додав рецепт {RecipeId} до обраного.", userId, recipeId);
            }

            return Result.Success();
        }

        public async Task<Result<List<Recipe>>> GetFavoriteRecipesAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний UserId ({UserId}) при отриманні обраних рецептів.", userId);
                return Result<List<Recipe>>.Failure("Некоректний ідентифікатор користувача.");
            }

            _logger.Information("Отримання списку обраних рецептів для користувача {UserId}.", userId);
            var recipes = await _favoriteRepository.GetFavoriteRecipesByUserIdAsync(userId);

            _logger.Information("Знайдено {Count} обраних рецептів для користувача {UserId}.", recipes.Count, userId);
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<bool>> IsFavoriteAsync(int userId, int recipeId)
        {
            if (userId <= 0 || recipeId <= 0)
            {
                return Result<bool>.Failure("Некоректні дані.");
            }

            bool isFav = await _favoriteRepository.IsFavoriteAsync(userId, recipeId);
            return Result<bool>.Success(isFav);
        }

        private static bool IsValidCookingTime(string? time)
        {
            if (string.IsNullOrWhiteSpace(time))
            {
                return false;
            }

            var match = Regex.Match(time.Trim(), @"^(\d{1,3}):([0-5]\d)$");
            if (!match.Success)
            {
                return false;
            }

            int hours = int.Parse(match.Groups[1].Value);
            int minutes = int.Parse(match.Groups[2].Value);
            return (hours * 60) + minutes > 0;
        }
    }
}