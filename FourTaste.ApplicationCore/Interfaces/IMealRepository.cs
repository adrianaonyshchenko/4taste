using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IMealPlanRepository
    {
        Task<MealPlan> AddAsync(MealPlan mealPlan);
        Task<List<MealPlan>> GetPlansByUserAndDateAsync(int userId, DateTime date);
        Task UpdateStatusAsync(int mealPlanId, bool isCooked);
        Task<MealPlan?> GetByIdAsync(int mealPlanId);
        Task DeleteAsync(int mealPlanId);
        Task<List<MealPlan>> GetCookingHistoryAsync(int userId, DateTime? date = null);
    }
}