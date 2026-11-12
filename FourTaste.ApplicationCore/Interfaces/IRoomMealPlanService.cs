using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRoomMealPlanService
    {
        Task<Result<List<RoomMealPlan>>> GetRoomMealPlansAsync(int roomId);
        Task<Result<int>> AddRoomMealPlanAsync(int roomId, int userId, string recipeName, int? recipeId, DateTime plannedDate);
        Task<Result> ToggleIsCookedAsync(int mealPlanId, int userId);
        Task<Result> DeleteRoomMealPlanAsync(int mealPlanId, int userId);
    }
}
