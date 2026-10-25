using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class RecipeService : IRecipeService
    {
        private const int MaxPhotoSizeBytes = 5 * 1024 * 1024;

        private readonly IRecipeRepository _recipeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger _logger;

        public RecipeService(IRecipeRepository recipeRepository, IUserRepository userRepository, ILogger logger)
        {
            _recipeRepository = recipeRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<int>> CreateRecipeAsync(
            string title,
            string time,
            string category,
            string cuisine,
            bool isPublic,
            int userId,
            List<string> ingredients,
            List<string> steps,
            byte[]? photo = null)
        {
            _logger.Information("Спроба створення нового рецепта: {Title}", title);

            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.Warning("Помилка валідації: назва рецепта порожня.");
                return Result<int>.Failure("Назва рецепта не може бути порожньою.");
            }

            if (ingredients == null || ingredients.Count == 0)
            {
                _logger.Warning("Помилка валідації: список інгредієнтів порожній.");
                return Result<int>.Failure("Додайте хоча б один інгредієнт.");
            }

            if (steps == null || steps.Count == 0)
            {
                _logger.Warning("Помилка валідації: список кроків порожній.");
                return Result<int>.Failure("Додайте кроки приготування.");
            }

            if (photo is { Length: > MaxPhotoSizeBytes })
            {
                _logger.Warning("Помилка валідації: фото завелике ({Size} байт).", photo.Length);
                return Result<int>.Failure("Фото завелике (максимум 5 МБ).");
            }

            var recipe = new Recipe
            {
                Title = title.Trim(),
                Time = time,
                Category = category,
                Cuisine = cuisine,
                IsPublic = isPublic,
                UserId = userId,
                Photo = photo,
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

            _logger.Information("Рецепт '{Title}' (Id {RecipeId}) успішно створено.", recipe.Title, recipe.Id);
            return Result<int>.Success(recipe.Id);
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
        public async Task<Result<List<Recipe>>> GetPublicRecipesAsync()
        {
            var recipes = await _recipeRepository.GetPublicRecipesAsync();
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<List<Recipe>>> GetRecipesByUserIdAsync(int userId)
        {
            var recipes = await _recipeRepository.GetRecipesByUserIdAsync(userId);
            return Result<List<Recipe>>.Success(recipes);
        }
    }
}