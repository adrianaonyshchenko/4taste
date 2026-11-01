using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class FridgeRepository : IFridgeRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public FridgeRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddAsync(UserFridgeItem item)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.UserFridgeItems.Add(item);
            await context.SaveChangesAsync();
        }

        public async Task RemoveAsync(int itemId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var item = await context.UserFridgeItems.FindAsync(itemId);
            if (item != null)
            {
                context.UserFridgeItems.Remove(item);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<UserFridgeItem>> GetByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserFridgeItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int userId, string ingredientName)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserFridgeItems
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.IngredientName.ToLower() == ingredientName.ToLower());
        }

        public async Task<UserFridgeItem?> GetByIdAsync(int itemId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserFridgeItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == itemId);
        }


        public async Task AddShoppingItemAsync(UserShoppingItem item)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.UserShoppingItems.Add(item);
            await context.SaveChangesAsync();
        }

        public async Task RemoveShoppingItemAsync(int itemId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var item = await context.UserShoppingItems.FindAsync(itemId);
            if (item != null)
            {
                context.UserShoppingItems.Remove(item);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<UserShoppingItem>> GetShoppingItemsByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserShoppingItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserShoppingItem?> GetShoppingItemByIdAsync(int itemId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserShoppingItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == itemId);
        }
    }
}