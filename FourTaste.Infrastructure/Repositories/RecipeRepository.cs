using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FourTaste.Infrastructure.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public RecipeRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddAsync(Recipe recipe)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Recipes.Add(recipe);
            await context.SaveChangesAsync();
        }

        public async Task<List<Recipe>> GetPublicRecipesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Recipes
                .Where(r => r.IsPublic == true)
                .ToListAsync();
        }

        public async Task<List<Recipe>> GetRecipesByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Recipes
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }
        public async Task<Recipe?> GetByIdAsync(int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Include(r => r.Steps.OrderBy(s => s.StepNumber))
                .FirstOrDefaultAsync(r => r.Id == recipeId);
        }
    }
}