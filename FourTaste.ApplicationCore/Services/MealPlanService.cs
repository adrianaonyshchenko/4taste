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
    public class MealPlanService
    {
        private readonly IMealPlanRepository _repository;
        private readonly ILogger _logger;

        public MealPlanService(IMealPlanRepository repository, ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<MealPlan>> AddCustomRecipeToPlanAsync(int userId, string recipeName, DateTime date)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба додати страву без валідного UserId: {UserId}", userId);
                return Result<MealPlan>.Failure("Користувач не визначений.");
            }

            if (string.IsNullOrWhiteSpace(recipeName))
            {
                _logger.Warning("Спроба додати порожню назву страви");
                return Result<MealPlan>.Failure("Введіть назву страви.");
            }

            var mealPlan = new MealPlan
            {
                UserId = userId,
                RecipeName = recipeName.Trim(),
                PlannedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                IsCooked = false,
            };

            var created = await _repository.AddAsync(mealPlan);

            _logger.Information("Користувач {UserId} успішно додав страву '{RecipeName}' на дату {Date}", userId, recipeName, date.ToShortDateString());

            return Result<MealPlan>.Success(created);
        }

        public async Task<List<MealPlan>> GetUserPlansForDateAsync(int userId, DateTime date)
        {
            if (userId <= 0)
            {
                return new List<MealPlan>();
            }

            return await _repository.GetPlansByUserAndDateAsync(userId, date);
        }

        public async Task<Result> UpdateMealPlanStatusAsync(int mealPlanId, int userId, bool isCooked)
        {
            if (mealPlanId <= 0)
            {
                _logger.Warning("Спроба позначити приготованим з некоректним Id {MealPlanId}", mealPlanId);
                return Result.Failure("Некоректний ідентифікатор запису меню.");
            }

            var plan = await _repository.GetByIdAsync(mealPlanId);

            if (plan is null)
            {
                _logger.Warning("Запис меню з Id {MealPlanId} не знайдено", mealPlanId);
                return Result.Failure("Запис меню не знайдено.");
            }

            if (plan.UserId != userId)
            {
                _logger.Warning("Користувач {UserId} спробував змінити чужий запис {MealPlanId}", userId, mealPlanId);
                return Result.Failure("Немає доступу до цього запису меню.");
            }

            if (plan.IsCooked == isCooked)
            {
                _logger.Warning("Запис {MealPlanId} вже має статус IsCooked={IsCooked}", mealPlanId, isCooked);
                return Result.Success();
            }

            await _repository.UpdateStatusAsync(mealPlanId, isCooked);

            _logger.Information("Користувач {UserId} змінив статус страви '{RecipeName}' на IsCooked={IsCooked} (запис {MealPlanId})", userId, plan.RecipeName, isCooked, mealPlanId);

            return Result.Success();
        }

        public async Task<Result> DeleteMealPlanAsync(int mealPlanId, int userId)
        {
            await _repository.DeleteAsync(mealPlanId);
            return Result.Success();
        }

        public async Task<Result<List<MealPlan>>> GetCookingHistoryAsync(int userId, DateTime? date = null)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба отримати історію готування без валідного UserId: {UserId}", userId);
                return Result<List<MealPlan>>.Failure("Користувач не визначений.");
            }

            var history = await _repository.GetCookingHistoryAsync(userId, date);
            _logger.Information("Отримано історію готування для користувача {UserId} (дата: {Date}, {Count} страв)", userId, date?.ToShortDateString() ?? "всі", history.Count);
            return Result<List<MealPlan>>.Success(history);
        }

        public async Task<Result<List<MealPlan>>> SearchCookingHistoryAsync(int userId, string? searchTerm)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба пошуку в історії готування без валідного UserId: {UserId}", userId);
                return Result<List<MealPlan>>.Failure("Користувач не визначений.");
            }

            var historyResult = await GetCookingHistoryAsync(userId);
            if (historyResult.IsFailure)
            {
                return historyResult;
            }

            var history = historyResult.Value ?? new List<MealPlan>();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Result<List<MealPlan>>.Success(history);
            }

            var query = searchTerm.Trim();
            var filtered = history
                .Where(m => !string.IsNullOrEmpty(m.RecipeName) && m.RecipeName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.Information("Знайдено {Count} страв в історії готування за запитом '{SearchTerm}'", filtered.Count, searchTerm);
            return Result<List<MealPlan>>.Success(filtered);
        }
    }
}
