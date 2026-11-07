using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IReviewRepository
    {
        Task<Recipe?> GetRecipeAccessAsync(int recipeId);
        Task<bool> ExistsAsync(int recipeId, int userId);
        Task<int> AddAsync(RecipeReview review);
        Task<bool> UpdateAsync(RecipeReview updated);
        Task<bool> DeleteAsync(int recipeId, int userId);
        Task<List<RecipeReview>> GetByRecipeAsync(int recipeId);
        Task<Dictionary<int, byte[]>> GetAuthorAvatarsAsync(IReadOnlyCollection<int> userIds);
    }
}