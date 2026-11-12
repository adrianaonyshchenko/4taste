using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRoomMealPlanRepository
    {
        Task<List<RoomMealPlan>> GetByRoomIdAsync(int roomId);
        Task<RoomMealPlan?> GetByIdAsync(int id);
        Task AddAsync(RoomMealPlan mealPlan);
        Task UpdateAsync(RoomMealPlan mealPlan);
        Task DeleteAsync(int id);
        Task<List<RoomMealPlan>> GetByRoomIdWithRecipesAsync(int roomId);
    }
}
