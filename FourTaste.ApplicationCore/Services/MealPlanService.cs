using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<MealPlan> AddCustomRecipeToPlanAsync(int userId, string recipeName, DateTime date)
        {
            if (userId <= 0)
            {
                _logger.Warning("Спроба додати страву без валідного UserId: {UserId}", userId);
                throw new ArgumentException("Користувач не визначений.");
            }

            if (string.IsNullOrWhiteSpace(recipeName))
            {
                _logger.Warning("Спроба додати порожню назву страви");
                throw new ArgumentException("Введіть назву страви.");
            }

            var mealPlan = new MealPlan
            {
                UserId = userId,
                RecipeName = recipeName.Trim(),
                PlannedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
                IsCooked = false
            };

            var created = await _repository.AddAsync(mealPlan);

            _logger.Information("Користувач {UserId} успішно додав страву '{RecipeName}' на дату {Date}", userId, recipeName, date.ToShortDateString());

            return created;
        }

        public async Task<List<MealPlan>> GetUserPlansForDateAsync(int userId, DateTime date)
        {
            if (userId <= 0)
            {
                return new List<MealPlan>();
            }

            return await _repository.GetPlansByUserAndDateAsync(userId, date);
        }
        public async Task UpdateMealPlanStatusAsync(int mealPlanId, bool isCooked)
        {
            await _repository.UpdateStatusAsync(mealPlanId, isCooked);
        }

    }

}