using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeService
    {
        IReadOnlyList<string> GetAvailableCuisines();

        IReadOnlyList<string> GetAvailableCategories();

        Task<Result<int>> CreateRecipeAsync(CreateRecipeRequest request);

        Task<Result> UpdateRecipeAsync(UpdateRecipeRequest request);

        Task<Result> DeleteRecipeAsync(int recipeId, int userId);

        Task<Result<List<Recipe>>> SearchRecipesAsync(RecipeSearchCriteria criteria);

        Task<Result<List<Recipe>>> FindRecipesByProductsAsync(FindRecipesByProductsRequest request);

        Task<Result<RecipeDetails>> GetRecipeDetailsAsync(int recipeId, int? viewerUserId);
    }
}
