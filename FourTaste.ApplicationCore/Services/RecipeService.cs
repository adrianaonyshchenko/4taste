using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogger _logger;

        public RecipeService(IRecipeRepository recipeRepository, IUserRepository userRepository, ILogger logger)
        {
            _recipeRepository = recipeRepository;
            _userRepository = userRepository;
            _logger = logger;
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

            if (request.Ingredients == null || request.Ingredients.Count == 0)
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

            foreach (var ingName in request.Ingredients)
            {
                recipe.Ingredients.Add(new Ingredient { Name = ingName });
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
    }
}