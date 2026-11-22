using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Data
{
    public class RecipeCategoryRepository : IRecipeCategoryRepository
    {
        private readonly AppDbContext _context;

        public RecipeCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecipeCategory>> GetAllAsync()
        {
            return await _context.RecipeCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<RecipeCategory?> GetByIdAsync(int id)
        {
            return await _context.RecipeCategories
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            string normalized = name.Trim().ToLowerInvariant();
            return await _context.RecipeCategories
                .AnyAsync(c =>
                    c.Name.ToLower() == normalized &&
                    (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<int> CountRecipesByCategoryNameAsync(string categoryName)
        {
            string normalized = categoryName.Trim().ToLowerInvariant();
            return await _context.Recipes
                .CountAsync(r => r.Category.ToLower() == normalized);
        }

        public async Task AddAsync(RecipeCategory category)
        {
            category.CreatedAt = category.CreatedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(category.CreatedAt, DateTimeKind.Utc)
                : category.CreatedAt.ToUniversalTime();

            await _context.RecipeCategories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RecipeCategory category)
        {
            category.CreatedAt = category.CreatedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(category.CreatedAt, DateTimeKind.Utc)
                : category.CreatedAt.ToUniversalTime();

            _context.RecipeCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task RenameCategoryInRecipesAsync(string oldName, string newName)
        {
            string normalizedOld = oldName.Trim().ToLowerInvariant();
            var recipes = await _context.Recipes
                .Where(r => r.Category.ToLower() == normalizedOld)
                .ToListAsync();

            foreach (var recipe in recipes)
            {
                recipe.Category = newName;
            }

            if (recipes.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var category = await this.GetByIdAsync(id);
            if (category != null)
            {
                _context.RecipeCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
