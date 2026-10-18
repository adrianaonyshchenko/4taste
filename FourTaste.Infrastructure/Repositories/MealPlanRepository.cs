using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class MealPlanRepository : IMealPlanRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public MealPlanRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<MealPlan> AddAsync(MealPlan mealPlan)
        {
            using var context = _contextFactory.CreateDbContext();
            await context.MealPlans.AddAsync(mealPlan);
            await context.SaveChangesAsync();
            return mealPlan;
        }

        public async Task<List<MealPlan>> GetPlansByUserAndDateAsync(int userId, DateTime date)
        {
            using var context = _contextFactory.CreateDbContext();
            var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return await context.MealPlans
                .Where(p => p.UserId == userId && p.PlannedDate.Date == utcDate.Date)
                .ToListAsync();
        }
        public async Task UpdateStatusAsync(int mealPlanId, bool isCooked)
        {
            using var context = _contextFactory.CreateDbContext();
            var plan = await context.MealPlans.FindAsync(mealPlanId);
            if (plan != null)
            {
                plan.IsCooked = isCooked;
                await context.SaveChangesAsync();
            }
        }

        public async Task<MealPlan?> GetByIdAsync(int mealPlanId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.MealPlans.FindAsync(mealPlanId);
        }

        public async Task DeleteAsync(int mealPlanId)
        {
            using var context = _contextFactory.CreateDbContext();
            var plan = await context.MealPlans.FindAsync(mealPlanId);
            if (plan != null)
            {
                context.MealPlans.Remove(plan);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<MealPlan>> GetCookingHistoryAsync(int userId, DateTime? date = null)
        {
            using var context = _contextFactory.CreateDbContext();
            var query = context.MealPlans.Where(p => p.UserId == userId && p.IsCooked);

            if (date.HasValue)
            {
                var utcDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                query = query.Where(p => p.PlannedDate.Date == utcDate.Date);
            }

            return await query
                .OrderByDescending(p => p.PlannedDate)
                .ToListAsync();
        }
    }
}