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
        private static readonly System.Threading.SemaphoreSlim _catalogLock = new System.Threading.SemaphoreSlim(1, 1);
        private static readonly System.Threading.SemaphoreSlim _substitutesLock = new System.Threading.SemaphoreSlim(1, 1);
        private static List<AllergenCatalogItem>? _cachedCatalog;
        private static List<IngredientSubstitute>? _cachedSubstitutes;

        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public SubstituteRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public static void ClearCache()
        {
            _cachedCatalog = null;
            _cachedSubstitutes = null;
        }

        public async Task<List<AllergenCatalogItem>> GetAllergenCatalogAsync()
        {
            if (_cachedCatalog != null)
            {
                return new List<AllergenCatalogItem>(_cachedCatalog);
            }

            await _catalogLock.WaitAsync();
            try
            {
                if (_cachedCatalog != null)
                {
                    return new List<AllergenCatalogItem>(_cachedCatalog);
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var items = await context.AllergenCatalogItems.AsNoTracking().ToListAsync();
                if (items.Count == 0)
                {
                    items = SubstituteSeedData.GetAllergens();
                }
                else
                {
                    var missing = SubstituteSeedData.GetAllergens()
                        .Where(s => !items.Any(i => i.Code == s.Code))
                        .ToList();

                    if (missing.Count > 0)
                    {
                        items.AddRange(missing);
                    }
                }

                _cachedCatalog = items;
                return new List<AllergenCatalogItem>(_cachedCatalog);
            }
            finally
            {
                _catalogLock.Release();
            }
        }

        public async Task<List<IngredientSubstitute>> GetAllSubstitutesAsync()
        {
            if (_cachedSubstitutes != null)
            {
                return new List<IngredientSubstitute>(_cachedSubstitutes);
            }

            await _substitutesLock.WaitAsync();
            try
            {
                if (_cachedSubstitutes != null)
                {
                    return new List<IngredientSubstitute>(_cachedSubstitutes);
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var list = await context.IngredientSubstitutes.AsNoTracking().ToListAsync();
                if (list.Count == 0)
                {
                    list = SubstituteSeedData.GetSubstitutes();
                }
                else
                {
                    var missing = SubstituteSeedData.GetSubstitutes()
                        .Where(s => !list.Any(i => i.OriginalIngredientName == s.OriginalIngredientName && i.SubstituteName == s.SubstituteName))
                        .ToList();

                    if (missing.Count > 0)
                    {
                        list.AddRange(missing);
                    }
                }

                _cachedSubstitutes = list;
                return new List<IngredientSubstitute>(_cachedSubstitutes);
            }
            finally
            {
                _substitutesLock.Release();
            }
        }

        public async Task<IngredientSubstitute?> GetSubstituteByIdAsync(int substituteId)
        {
            if (_cachedSubstitutes != null)
            {
                var cached = _cachedSubstitutes.FirstOrDefault(s => s.Id == substituteId);
                if (cached != null)
                {
                    return cached;
                }
            }

            var all = await GetAllSubstitutesAsync();
            return all.FirstOrDefault(s => s.Id == substituteId);
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

