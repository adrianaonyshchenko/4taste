using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeService
    {
        Task<Result<int>> CreateRecipeAsync(CreateRecipeRequest request);

        Task<Result<List<Recipe>>> SearchRecipesAsync(RecipeSearchCriteria criteria);

        Task<Result<RecipeDetails>> GetRecipeDetailsAsync(int recipeId, int? viewerUserId);
    }
}