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
    }
}