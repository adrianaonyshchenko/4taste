using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class SubstituteRepository : ISubstituteRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public SubstituteRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<AllergenCatalogItem>> GetAllergenCatalogAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var items = await context.AllergenCatalogItems.AsNoTracking().ToListAsync();
            if (items.Count == 0)
            {
                return SubstituteSeedData.GetAllergens();
            }

            var missing = SubstituteSeedData.GetAllergens()
                .Where(s => !items.Any(i => i.Code == s.Code))
                .ToList();

            if (missing.Count > 0)
            {
                items.AddRange(missing);
            }

            return items;
        }

        public async Task<List<IngredientSubstitute>> GetAllSubstitutesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var list = await context.IngredientSubstitutes.AsNoTracking().ToListAsync();
            if (list.Count == 0)
            {
                return SubstituteSeedData.GetSubstitutes();
            }

            var missing = SubstituteSeedData.GetSubstitutes()
                .Where(s => !list.Any(i => i.OriginalIngredientName == s.OriginalIngredientName && i.SubstituteName == s.SubstituteName))
                .ToList();

            if (missing.Count > 0)
            {
                list.AddRange(missing);
            }

            return list;
        }

        public async Task<IngredientSubstitute?> GetSubstituteByIdAsync(int substituteId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var item = await context.IngredientSubstitutes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == substituteId);
            if (item == null)
            {
                var seeds = SubstituteSeedData.GetSubstitutes();
                item = seeds.FirstOrDefault(s => s.Id == substituteId);
            }

            return item;
        }

        public async Task<List<UserRecipeSubstitution>> GetUserRecipeSubstitutionsAsync(int userId, int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserRecipeSubstitutions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.RecipeId == recipeId)
                .ToListAsync();
        }

        public async Task<UserRecipeSubstitution?> GetUserRecipeSubstitutionAsync(int userId, int recipeId, int ingredientId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserRecipeSubstitutions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.RecipeId == recipeId && s.OriginalIngredientId == ingredientId);
        }

        public async Task SaveUserRecipeSubstitutionAsync(UserRecipeSubstitution substitution)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.UserRecipeSubstitutions
                .FirstOrDefaultAsync(s => s.UserId == substitution.UserId &&
                                          s.RecipeId == substitution.RecipeId &&
                                          s.OriginalIngredientId == substitution.OriginalIngredientId);

            if (existing != null)
            {
                existing.SubstituteIngredientName = substitution.SubstituteIngredientName;
                existing.CalculatedAmount = substitution.CalculatedAmount;
                existing.Ratio = substitution.Ratio;
                existing.Note = substitution.Note;
                existing.OriginalIngredientName = substitution.OriginalIngredientName;
                context.UserRecipeSubstitutions.Update(existing);
            }
            else
            {
                context.UserRecipeSubstitutions.Add(substitution);
            }

            await context.SaveChangesAsync();
        }

        public async Task RemoveUserRecipeSubstitutionAsync(int userId, int recipeId, int ingredientId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.UserRecipeSubstitutions
                .FirstOrDefaultAsync(s => s.UserId == userId &&
                                          s.RecipeId == recipeId &&
                                          s.OriginalIngredientId == ingredientId);

            if (existing != null)
            {
                context.UserRecipeSubstitutions.Remove(existing);
                await context.SaveChangesAsync();
            }
        }
    }
}

