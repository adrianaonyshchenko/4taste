using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeService
    {
        Task<Result<int>> CreateRecipeAsync(string title, string time, string category, string cuisine, bool isPublic, int userId, List<string> ingredients, List<string> steps, byte[]? photo = null);
        Task<Result<RecipeDetails>> GetRecipeDetailsAsync(int recipeId, int? viewerUserId);
    }
}