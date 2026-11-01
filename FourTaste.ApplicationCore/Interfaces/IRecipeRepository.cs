using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeRepository
    {
        Task AddAsync(Recipe recipe);

        Task<Recipe?> GetByIdAsync(int recipeId);

        Task<List<Recipe>> SearchAsync(RecipeSearchCriteria criteria);
    }
}