using System;
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
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger _logger;

        public ReviewService(IReviewRepository reviewRepository, ILogger logger)
        {
            _reviewRepository = reviewRepository;
            _logger = logger;
        }

        public IReadOnlyList<string> GetAvailableTags()
        {
            return ReviewOptions.Tags;
        }

        public async Task<Result<int>> AddReviewAsync(ReviewRequest request)
        {
            var validation = ValidateRequest(request);
            if (validation.IsFailure)
            {
                return Result<int>.Failure(validation.Error!);
            }

            var recipeResult = await GetAccessibleRecipeAsync(request.RecipeId, request.UserId);
            if (recipeResult.IsFailure)
            {
                return Result<int>.Failure(recipeResult.Error!);
            }

            if (recipeResult.Value!.UserId == request.UserId)
            {
                _logger.Warning("Користувач {UserId} спробував оцінити власний рецепт {RecipeId}.", request.UserId, request.RecipeId);
                return Result<int>.Failure("Не можна залишати відгук на власний рецепт.");
            }

            if (await _reviewRepository.ExistsAsync(request.RecipeId, request.UserId))
            {
                _logger.Warning("Користувач {UserId} вже залишав відгук на рецепт {RecipeId}.", request.UserId, request.RecipeId);
                return Result<int>.Failure("Ви вже залишили відгук на цей рецепт.");
            }

            int reviewId = await _reviewRepository.AddAsync(BuildReview(request, validation.Value!));

            _logger.Information(
                "Користувач {UserId} залишив відгук {ReviewId} на рецепт {RecipeId} (оцінка {Rating}, тегів {TagCount}, фото: {HasPhoto}).",
                request.UserId,
                reviewId,
                request.RecipeId,
                request.Rating,
                validation.Value!.Count,
                request.Photo is not null);

            return Result<int>.Success(reviewId);
        }

        public async Task<Result> UpdateReviewAsync(ReviewRequest request)
        {
            var validation = ValidateRequest(request);
            if (validation.IsFailure)
            {
                return Result.Failure(validation.Error!);
            }

            var recipeResult = await GetAccessibleRecipeAsync(request.RecipeId, request.UserId);
            if (recipeResult.IsFailure)
            {
                return Result.Failure(recipeResult.Error!);
            }

            if (!await _reviewRepository.UpdateAsync(BuildReview(request, validation.Value!)))
            {
                _logger.Warning("Користувач {UserId} не має відгуку на рецепт {RecipeId} для редагування.", request.UserId, request.RecipeId);
                return Result.Failure("Відгук не знайдено.");
            }

            _logger.Information(
                "Користувач {UserId} змінив відгук на рецепт {RecipeId} (оцінка {Rating}, тегів {TagCount}, фото: {HasPhoto}).",
                request.UserId,
                request.RecipeId,
                request.Rating,
                validation.Value!.Count,
                request.Photo is not null);

            return Result.Success();
        }

        public async Task<Result> DeleteReviewAsync(int recipeId, int userId)
        {
            if (recipeId <= 0 || userId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректні дані для видалення відгуку (RecipeId: {RecipeId}, UserId: {UserId}).", recipeId, userId);
                return Result.Failure("Некоректні дані для видалення відгуку.");
            }

            if (!await _reviewRepository.DeleteAsync(recipeId, userId))
            {
                _logger.Warning("Користувач {UserId} не має відгуку на рецепт {RecipeId} для видалення.", userId, recipeId);
                return Result.Failure("Відгук не знайдено.");
            }

            _logger.Information("Користувач {UserId} видалив свій відгук на рецепт {RecipeId}.", userId, recipeId);
            return Result.Success();
        }

        public async Task<Result<ReviewsOverview>> GetOverviewAsync(int recipeId, int? viewerUserId)
        {
            var recipeResult = await GetAccessibleRecipeAsync(recipeId, viewerUserId);
            if (recipeResult.IsFailure)
            {
                return Result<ReviewsOverview>.Failure(recipeResult.Error!);
            }

            var reviews = await _reviewRepository.GetByRecipeAsync(recipeId);

            _logger.Information(
                "Відгуки рецепта {RecipeId} переглянуто (глядач: {ViewerUserId}, відгуків: {Count}).",
                recipeId,
                viewerUserId,
                reviews.Count);

            return Result<ReviewsOverview>.Success(new ReviewsOverview
            {
                IsRecipeOwner = viewerUserId.HasValue && recipeResult.Value!.UserId == viewerUserId.Value,
                Reviews = reviews,
            });
        }

        public async Task<Result<Dictionary<int, byte[]>>> GetAuthorAvatarsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
            {
                return Result<Dictionary<int, byte[]>>.Success(new Dictionary<int, byte[]>());
            }

            var avatars = await _reviewRepository.GetAuthorAvatarsAsync(ids);

            _logger.Information("Завантажено аватарки {Found} з {Requested} авторів відгуків.", avatars.Count, ids.Count);
            return Result<Dictionary<int, byte[]>>.Success(avatars);
        }
        private static RecipeReview BuildReview(ReviewRequest request, IEnumerable<string> tags)
        {
            return new RecipeReview
            {
                RecipeId = request.RecipeId,
                UserId = request.UserId,
                Rating = request.Rating,
                Text = request.Text?.Trim() ?? string.Empty,
                Photo = request.Photo,
                CreatedAt = DateTime.UtcNow,
                Tags = tags.Select(t => new ReviewTag { Name = t }).ToList(),
            };
        }

        private Result<List<string>> ValidateRequest(ReviewRequest? request)
        {
            if (request is null)
            {
                _logger.Warning("Помилка валідації: запит на збереження відгуку порожній.");
                return Result<List<string>>.Failure("Дані відгуку не задано.");
            }

            if (request.UserId <= 0)
            {
                _logger.Warning("Спроба зберегти відгук без авторизації (UserId: {UserId}).", request.UserId);
                return Result<List<string>>.Failure("Увійдіть в акаунт, щоб залишити відгук.");
            }

            if (request.Rating < ReviewOptions.MinRating || request.Rating > ReviewOptions.MaxRating)
            {
                _logger.Warning("Помилка валідації: оцінка {Rating} поза діапазоном.", request.Rating);
                return Result<List<string>>.Failure($"Оцінка має бути від {ReviewOptions.MinRating} до {ReviewOptions.MaxRating}.");
            }

            int textLength = request.Text?.Trim().Length ?? 0;
            if (textLength > ReviewOptions.MaxTextLength)
            {
                _logger.Warning("Помилка валідації: текст відгуку завеликий ({Length} символів).", textLength);
                return Result<List<string>>.Failure($"Текст відгуку не може бути довшим за {ReviewOptions.MaxTextLength} символів.");
            }

            if (request.Photo is { Length: > ReviewOptions.MaxPhotoSizeBytes })
            {
                _logger.Warning("Помилка валідації: фото відгуку завелике ({Size} байт).", request.Photo.Length);
                return Result<List<string>>.Failure("Фото завелике (максимум 1 МБ).");
            }

            return NormalizeTags(request.Tags);
        }

        private Result<List<string>> NormalizeTags(IEnumerable<string>? rawTags)
        {
            var tags = new List<string>();

            foreach (var raw in rawTags ?? Enumerable.Empty<string>())
            {
                string? canonical = ReviewOptions.Tags.FirstOrDefault(
                    t => string.Equals(t, raw?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (canonical is null)
                {
                    _logger.Warning("Помилка валідації: невідомий тег '{Tag}'.", raw);
                    return Result<List<string>>.Failure($"Невідомий тег: «{raw}».");
                }

                if (!tags.Contains(canonical))
                {
                    tags.Add(canonical);
                }
            }

            if (tags.Count > ReviewOptions.MaxTags)
            {
                _logger.Warning("Помилка валідації: вибрано {Count} тегів, максимум {Max}.", tags.Count, ReviewOptions.MaxTags);
                return Result<List<string>>.Failure($"Можна вибрати не більше {ReviewOptions.MaxTags} тегів.");
            }

            return Result<List<string>>.Success(tags);
        }

        private async Task<Result<Recipe>> GetAccessibleRecipeAsync(int recipeId, int? viewerUserId)
        {
            if (recipeId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний Id рецепта {RecipeId}.", recipeId);
                return Result<Recipe>.Failure("Некоректний ідентифікатор рецепта.");
            }

            var recipe = await _reviewRepository.GetRecipeAccessAsync(recipeId);
            if (recipe is null)
            {
                _logger.Warning("Рецепт {RecipeId} не знайдено.", recipeId);
                return Result<Recipe>.Failure("Рецепт не знайдено.");
            }

            if (!recipe.IsPublic && recipe.UserId != viewerUserId)
            {
                _logger.Warning("Немає доступу до приватного рецепта {RecipeId} (глядач: {ViewerUserId}).", recipeId, viewerUserId);
                return Result<Recipe>.Failure("Цей рецепт приватний.");
            }

            return Result<Recipe>.Success(recipe);
        }
    }
}