using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeService
    {
        Task<Result<int>> CreateRecipeAsync(
            string title,
            string time,
            string category,
            string cuisine,
            bool isPublic,
            int userId,
            List<string> ingredients,
            List<string> steps,
            byte[]? photo = null);

        Task<Result<List<Recipe>>> GetPublicRecipesAsync();

        Task<Result<List<Recipe>>> SearchPublicRecipesAsync(string? searchTerm, RecipeSearchFilter filter = RecipeSearchFilter.All);

        Task<Result<List<Recipe>>> GetRecipesByUserIdAsync(int userId);

        Task<Result<List<Recipe>>> SearchUserRecipesAsync(int userId, string? searchTerm);

        Task<Result<RecipeDetails>> GetRecipeDetailsAsync(int recipeId, int? viewerUserId);
    }
}
