using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public FavoriteRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddFavoriteAsync(int userId, int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            bool exists = await context.UserFavoriteRecipes
                .AnyAsync(x => x.UserId == userId && x.RecipeId == recipeId);

            if (!exists)
            {
                context.UserFavoriteRecipes.Add(new UserFavoriteRecipe
                {
                    UserId = userId,
                    RecipeId = recipeId
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveFavoriteAsync(int userId, int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.UserFavoriteRecipes
                .FirstOrDefaultAsync(x => x.UserId == userId && x.RecipeId == recipeId);

            if (entity != null)
            {
                context.UserFavoriteRecipes.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.UserFavoriteRecipes
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.RecipeId == recipeId);
        }

        public async Task<List<Recipe>> GetFavoriteRecipesByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var recipeIds = await context.UserFavoriteRecipes
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.RecipeId)
                .ToListAsync();

            if (recipeIds.Count == 0)
            {
                return new List<Recipe>();
            }

            return await context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .Where(r => recipeIds.Contains(r.Id))
                .ToListAsync();
        }
    }
}
