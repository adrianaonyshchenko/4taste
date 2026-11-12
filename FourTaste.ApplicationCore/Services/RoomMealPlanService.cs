using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class RoomMealPlanService : IRoomMealPlanService
    {
        private readonly IRoomMealPlanRepository _mealPlanRepository;
        private readonly ILogger _logger;

        public RoomMealPlanService(IRoomMealPlanRepository mealPlanRepository, ILogger logger)
        {
            _mealPlanRepository = mealPlanRepository;
            _logger = logger;
        }

        public async Task<Result<List<RoomMealPlan>>> GetRoomMealPlansAsync(int roomId)
        {
            if (roomId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний RoomId ({RoomId}) при отриманні спільного меню.", roomId);
                return Result<List<RoomMealPlan>>.Failure("Некоректний ідентифікатор кімнати.");
            }

            _logger.Information("Отримання спільного меню для кімнати {RoomId}.", roomId);
            var plans = await _mealPlanRepository.GetByRoomIdAsync(roomId);

            return Result<List<RoomMealPlan>>.Success(plans);
        }

        public async Task<Result<int>> AddRoomMealPlanAsync(int roomId, int userId, string recipeName, int? recipeId, DateTime plannedDate)
        {
            if (roomId <= 0 || userId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний RoomId ({RoomId}) або UserId ({UserId}).", roomId, userId);
                return Result<int>.Failure("Некоректні дані запиту.");
            }

            if (string.IsNullOrWhiteSpace(recipeName))
            {
                _logger.Warning("Помилка валідації: назва страви/рецепта порожня для кімнати {RoomId}.", roomId);
                return Result<int>.Failure("Назва страви не може бути порожньою.");
            }

            var plan = new RoomMealPlan
            {
                RoomId = roomId,
                UserId = userId,
                RecipeName = recipeName.Trim(),
                RecipeId = recipeId,
                PlannedDate = plannedDate,
                IsCooked = false
            };

            await _mealPlanRepository.AddAsync(plan);
            _logger.Information("Успішно додано страву '{RecipeName}' до спільного меню кімнати {RoomId} на дату {Date}.", plan.RecipeName, roomId, plannedDate.ToShortDateString());

            return Result<int>.Success(plan.Id);
        }

        public async Task<Result> ToggleIsCookedAsync(int mealPlanId, int userId)
        {
            if (mealPlanId <= 0 || userId <= 0)
            {
                _logger.Warning("Помилка валідації: некоректний MealPlanId ({MealPlanId}) або UserId ({UserId}).", mealPlanId, userId);
                return Result.Failure("Некоректні дані запиту.");
            }

            var plan = await _mealPlanRepository.GetByIdAsync(mealPlanId);
            if (plan == null)
            {
                _logger.Warning("Спільне меню: запис з Id {MealPlanId} не знайдено.", mealPlanId);
                return Result.Failure("Запис у меню не знайдено.");
            }

            plan.IsCooked = !plan.IsCooked;
            await _mealPlanRepository.UpdateAsync(plan);

            _logger.Information("Статус приготування страви (Id: {MealPlanId}) змінено на {IsCooked} користувачем {UserId}.", mealPlanId, plan.IsCooked, userId);
            return Result.Success();
        }

        public async Task<Result> DeleteRoomMealPlanAsync(int mealPlanId, int userId)
        {
            if (mealPlanId <= 0 || userId <= 0)
            {
                _logger.Warning("Помилка валідації при видаленні плану: MealPlanId ({MealPlanId}), UserId ({UserId}).", mealPlanId, userId);
                return Result.Failure("Некоректні дані для видалення.");
            }

            var plan = await _mealPlanRepository.GetByIdAsync(mealPlanId);
            if (plan == null)
            {
                _logger.Warning("Видалення спільного меню: запис {MealPlanId} не знайдено.", mealPlanId);
                return Result.Failure("Запис у меню не знайдено.");
            }

            await _mealPlanRepository.DeleteAsync(mealPlanId);
            _logger.Information("Запис спільного меню {MealPlanId} успішно видалено користувачем {UserId}.", mealPlanId, userId);

            return Result.Success();
        }
    }
}