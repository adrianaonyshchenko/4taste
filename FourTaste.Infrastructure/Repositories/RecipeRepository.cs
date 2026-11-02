using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private const string UnknownAuthor = "Невідомий автор";
        private const string TimePattern = @"^\d+:\d{2}$";

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

        public async Task<Recipe?> GetByIdAsync(int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Include(r => r.Steps.OrderBy(s => s.StepNumber))
                .FirstOrDefaultAsync(r => r.Id == recipeId);
        }

        public async Task UpdateAsync(Recipe recipe)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existing is null)
            {
                return;
            }

            existing.Title = recipe.Title;
            existing.Time = recipe.Time;
            existing.Category = recipe.Category;
            existing.Cuisine = recipe.Cuisine;
            existing.IsPublic = recipe.IsPublic;
            existing.Servings = recipe.Servings;
            existing.Photo = recipe.Photo;

            context.RemoveRange(existing.Ingredients);
            existing.Ingredients = recipe.Ingredients.Select(i => new Ingredient
            {
                Name = i.Name.Trim(),
                Amount = i.Amount.Trim(),
                RecipeId = recipe.Id,
            }).ToList();

            context.RemoveRange(existing.Steps);
            int stepNum = 1;
            existing.Steps = recipe.Steps.Select(s => new RecipeStep
            {
                StepNumber = stepNum++,
                Description = s.Description.Trim(),
                RecipeId = recipe.Id,
            }).ToList();

            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (existing != null)
            {
                context.Recipes.Remove(existing);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Recipe>> SearchAsync(RecipeSearchCriteria criteria)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var query =
                from r in context.Recipes.AsNoTracking().Include(r => r.Ingredients)
                join u in context.Users.AsNoTracking() on r.UserId equals u.ID into authors
                from a in authors.DefaultIfEmpty()
                select new RecipeRow
                {
                    Recipe = r,
                    AuthorId = (int?)a.ID,
                    AuthorUsername = a.Username,
                    AuthorName = a.Name,
                };

            if (criteria.OnlyPublic)
            {
                query = query.Where(x => x.Recipe.IsPublic);
            }

            if (criteria.OwnerUserId is int ownerId)
            {
                query = query.Where(x => x.Recipe.UserId == ownerId);
            }

            if (criteria.ViewerUserId is int viewerId)
            {
                query = query.Where(x => x.Recipe.IsPublic || x.Recipe.UserId == viewerId);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Category))
            {
                var category = criteria.Category.Trim().ToLower();
                query = query.Where(x => x.Recipe.Category.ToLower() == category);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Cuisine))
            {
                var cuisine = criteria.Cuisine.Trim().ToLower();
                query = query.Where(x => x.Recipe.Cuisine.ToLower() == cuisine);
            }

            if (criteria.MaxCookingMinutes is int maxMinutes)
            {
                int maxSeconds = maxMinutes * 60;
                query = query.Where(x =>
                    (Regex.IsMatch(x.Recipe.Time, TimePattern)
                        ? (Convert.ToInt32(x.Recipe.Time.Substring(0, x.Recipe.Time.Length - 3)) * 60)
                            + Convert.ToInt32(x.Recipe.Time.Substring(x.Recipe.Time.Length - 2))
                        : int.MaxValue) <= maxSeconds);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
            {
                var term = criteria.SearchTerm.Trim().ToLower();
                var termWithoutAt = term.StartsWith('@') && term.Length > 1 ? term.Substring(1) : term;

                query = criteria.Filter switch
                {
                    RecipeSearchFilter.TitleOnly => query.Where(x =>
                        x.Recipe.Title.ToLower().Contains(term)),

                    RecipeSearchFilter.AuthorOnly => query.Where(x =>
                        (x.AuthorName ?? string.Empty).ToLower().Contains(term) ||
                        (x.AuthorUsername ?? string.Empty).ToLower().Contains(term) ||
                        (x.AuthorUsername ?? string.Empty).ToLower().Contains(termWithoutAt)),

                    _ => query.Where(x =>
                        x.Recipe.Title.ToLower().Contains(term) ||
                        x.Recipe.Category.ToLower().Contains(term) ||
                        x.Recipe.Cuisine.ToLower().Contains(term) ||
                        (x.AuthorName ?? string.Empty).ToLower().Contains(term) ||
                        (x.AuthorUsername ?? string.Empty).ToLower().Contains(term) ||
                        (x.AuthorUsername ?? string.Empty).ToLower().Contains(termWithoutAt)),
                };
            }

            var allergens = Normalize(criteria.ExcludedAllergens);
            var products = Normalize(criteria.AvailableProducts);
            bool byProducts = products.Count > 0;

            var rows = await query.ToListAsync();

            if (allergens.Count > 0)
            {
                rows = rows
                    .Where(x => !x.Recipe.Ingredients.Any(i => allergens.Any(al => i.Name.ToLower().Contains(al))))
                    .ToList();
            }

            if (byProducts)
            {
                rows = rows
                    .Where(x => x.Recipe.Ingredients.Any(i => products.Any(p => i.Name.ToLower().Contains(p))))
                    .Select(x =>
                    {
                        x.MatchedCount = x.Recipe.Ingredients.Count(i => products.Any(p => i.Name.ToLower().Contains(p)));
                        x.TotalCount = x.Recipe.Ingredients.Count;
                        return x;
                    })
                    .OrderBy(x => x.TotalCount - x.MatchedCount)
                    .ThenByDescending(x => x.MatchedCount)
                    .ToList();
            }

            foreach (var row in rows)
            {
                if (row.AuthorId is null)
                {
                    row.Recipe.AuthorUsername = string.Empty;
                    row.Recipe.AuthorName = UnknownAuthor;
                }
                else
                {
                    row.Recipe.AuthorUsername = row.AuthorUsername ?? string.Empty;
                    row.Recipe.AuthorName = string.IsNullOrWhiteSpace(row.AuthorName)
                        ? row.AuthorUsername ?? string.Empty
                        : row.AuthorName;
                }

                row.Recipe.MatchedIngredientsCount = row.MatchedCount;
                row.Recipe.TotalIngredientsCount = row.TotalCount;
            }

            return rows.Select(x => x.Recipe).ToList();
        }

        private static List<string> Normalize(IEnumerable<string>? values)
        {
            return values?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim().ToLower())
                .Distinct()
                .ToList() ?? new List<string>();
        }

        private sealed class RecipeRow
        {
            public Recipe Recipe { get; set; } = null!;
            public int? AuthorId { get; set; }
            public string? AuthorUsername { get; set; }
            public string? AuthorName { get; set; }
            public int MatchedCount { get; set; }
            public int TotalCount { get; set; }
        }
    }
}
