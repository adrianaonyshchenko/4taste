using System;
using System.Collections.Generic;
using System.Linq;
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

            if (photo != null && photo.Length > MaxPhotoSizeBytes)
            {
                _logger.Warning("Помилка валідації: розмір фото перевищує 5 МБ.");
                return Result<int>.Failure("Розмір фотографії не може перевищувати 5 МБ.");
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
            await PopulateAuthorsAsync(recipes);
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<List<Recipe>>> SearchPublicRecipesAsync(string? searchTerm, RecipeSearchFilter filter = RecipeSearchFilter.All)
        {
            _logger.Information("Пошук публічних рецептів за запитом: '{SearchTerm}', фільтр: {Filter}", searchTerm, filter);

            var recipes = await _recipeRepository.GetPublicRecipesAsync();
            await PopulateAuthorsAsync(recipes);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Result<List<Recipe>>.Success(recipes);
            }

            var query = searchTerm.Trim();
            var queryWithoutAt = query.StartsWith('@') ? query.Substring(1) : query;

            var filtered = recipes.Where(r =>
            {
                bool matchTitle = filter != RecipeSearchFilter.AuthorOnly &&
                    !string.IsNullOrEmpty(r.Title) &&
                    r.Title.Contains(query, StringComparison.OrdinalIgnoreCase);

                bool matchAuthorName = filter != RecipeSearchFilter.TitleOnly &&
                    !string.IsNullOrEmpty(r.AuthorName) &&
                    r.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase);

                bool matchAuthorUsername = filter != RecipeSearchFilter.TitleOnly &&
                    !string.IsNullOrEmpty(r.AuthorUsername) &&
                    (r.AuthorUsername.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     r.AuthorUsername.Contains(queryWithoutAt, StringComparison.OrdinalIgnoreCase));

                return matchTitle || matchAuthorName || matchAuthorUsername;
            }).ToList();

            _logger.Information("Знайдено {Count} рецептів за запитом '{SearchTerm}'", filtered.Count, searchTerm);
            return Result<List<Recipe>>.Success(filtered);
        }

        public async Task<Result<List<Recipe>>> GetRecipesByUserIdAsync(int userId)
        {
            var recipes = await _recipeRepository.GetRecipesByUserIdAsync(userId);
            await PopulateAuthorsAsync(recipes);
            return Result<List<Recipe>>.Success(recipes);
        }

        public async Task<Result<List<Recipe>>> SearchUserRecipesAsync(int userId, string? searchTerm)
        {
            _logger.Information("Пошук власних рецептів користувача {UserId} за запитом: '{SearchTerm}'", userId, searchTerm);

            var recipes = await _recipeRepository.GetRecipesByUserIdAsync(userId);
            await PopulateAuthorsAsync(recipes);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Result<List<Recipe>>.Success(recipes);
            }

            var query = searchTerm.Trim();
            var filtered = recipes.Where(r =>
                (!string.IsNullOrEmpty(r.Title) && r.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.Category) && r.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.Cuisine) && r.Cuisine.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            _logger.Information("Знайдено {Count} власних рецептів за запитом '{SearchTerm}'", filtered.Count, searchTerm);
            return Result<List<Recipe>>.Success(filtered);
        }

        private async Task PopulateAuthorsAsync(List<Recipe> recipes)
        {
            if (recipes == null || recipes.Count == 0)
            {
                return;
            }

            var users = await _userRepository.ListAsync();
            var userMap = users?.ToDictionary(u => u.ID, u => u) ?? new Dictionary<int, User>();

            foreach (var recipe in recipes)
            {
                if (userMap.TryGetValue(recipe.UserId, out var author) && author != null)
                {
                    recipe.AuthorUsername = author.Username ?? string.Empty;
                    recipe.AuthorName = string.IsNullOrWhiteSpace(author.Name) ? (author.Username ?? string.Empty) : author.Name;
                }
                else
                {
                    recipe.AuthorUsername = string.Empty;
                    recipe.AuthorName = "Невідомий автор";
                }
            }
        }
    }
}
