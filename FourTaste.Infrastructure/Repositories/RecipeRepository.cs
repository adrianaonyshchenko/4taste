using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<Recipe>> SearchAsync(RecipeSearchCriteria criteria)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var query =
                from r in context.Recipes.AsNoTracking()
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

            var rows = await query.ToListAsync();

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
            }

            return rows.Select(x => x.Recipe).ToList();
        }

        private sealed class RecipeRow
        {
            public Recipe Recipe { get; set; } = null!;
            public int? AuthorId { get; set; }
            public string? AuthorUsername { get; set; }
            public string? AuthorName { get; set; }
        }
    }
}