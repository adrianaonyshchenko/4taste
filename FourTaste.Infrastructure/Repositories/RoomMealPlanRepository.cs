using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Data
{
    public class RoomMealPlanRepository : IRoomMealPlanRepository
    {
        private readonly AppDbContext _context;

        public RoomMealPlanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoomMealPlan>> GetByRoomIdAsync(int roomId)
        {
            return await _context.RoomMealPlans
                .Where(m => m.RoomId == roomId)
                .ToListAsync();
        }

        public async Task<RoomMealPlan?> GetByIdAsync(int id)
        {
            return await _context.RoomMealPlans
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddAsync(RoomMealPlan mealPlan)
        {
            if (mealPlan.PlannedDate.Kind == DateTimeKind.Unspecified)
            {
                mealPlan.PlannedDate = DateTime.SpecifyKind(mealPlan.PlannedDate, DateTimeKind.Utc);
            }
            else
            {
                mealPlan.PlannedDate = mealPlan.PlannedDate.ToUniversalTime();
            }

            await _context.RoomMealPlans.AddAsync(mealPlan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RoomMealPlan mealPlan)
        {
            if (mealPlan.PlannedDate.Kind == DateTimeKind.Unspecified)
            {
                mealPlan.PlannedDate = DateTime.SpecifyKind(mealPlan.PlannedDate, DateTimeKind.Utc);
            }
            else
            {
                mealPlan.PlannedDate = mealPlan.PlannedDate.ToUniversalTime();
            }

            _context.RoomMealPlans.Update(mealPlan);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var plan = await GetByIdAsync(id);
            if (plan != null)
            {
                _context.RoomMealPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }
        }
    }
}