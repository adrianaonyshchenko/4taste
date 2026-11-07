using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ReviewRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Recipe?> GetRecipeAccessAsync(int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Recipes
                .AsNoTracking()
                .Where(r => r.Id == recipeId)
                .Select(r => new Recipe { Id = r.Id, UserId = r.UserId, IsPublic = r.IsPublic })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(int recipeId, int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RecipeReviews
                .AsNoTracking()
                .AnyAsync(r => r.RecipeId == recipeId && r.UserId == userId);
        }

        public async Task<int> AddAsync(RecipeReview review)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.RecipeReviews.Add(review);
            await context.SaveChangesAsync();
            return review.Id;
        }

        public async Task<bool> UpdateAsync(RecipeReview updated)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.RecipeReviews
                .Include(r => r.Tags)
                .FirstOrDefaultAsync(r => r.RecipeId == updated.RecipeId && r.UserId == updated.UserId);

            if (existing is null)
            {
                return false;
            }

            existing.Rating = updated.Rating;
            existing.Text = updated.Text;
            existing.Photo = updated.Photo;

            context.ReviewTags.RemoveRange(existing.Tags);
            foreach (var tag in updated.Tags)
            {
                context.ReviewTags.Add(new ReviewTag { ReviewId = existing.Id, Name = tag.Name });
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int recipeId, int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var review = await context.RecipeReviews
                .Include(r => r.Tags)
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (review is null)
            {
                return false;
            }

            context.RecipeReviews.Remove(review);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RecipeReview>> GetByRecipeAsync(int recipeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var reviews = await context.RecipeReviews
                .AsNoTracking()
                .Include(r => r.Tags)
                .AsSplitQuery()
                .Where(r => r.RecipeId == recipeId)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync();

            var userIds = reviews.Select(r => r.UserId).Distinct().ToList();

            var usernames = await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.ID))
                .Select(u => new { u.ID, u.Username })
                .ToDictionaryAsync(u => u.ID, u => u.Username);

            foreach (var review in reviews)
            {
                review.AuthorName = usernames.TryGetValue(review.UserId, out var username)
                    ? username
                    : "Невідомий користувач";
            }

            return reviews;
        }

        public async Task<Dictionary<int, byte[]>> GetAuthorAvatarsAsync(IReadOnlyCollection<int> userIds)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var rows = await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.ID) && u.Avatar != null)
                .Select(u => new { u.ID, u.Avatar })
                .ToListAsync();

            return rows.ToDictionary(x => x.ID, x => x.Avatar!);
        }
    }
}