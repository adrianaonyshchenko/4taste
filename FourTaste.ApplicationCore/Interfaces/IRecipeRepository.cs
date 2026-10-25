using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeRepository
    {
        Task AddAsync(Recipe recipe);
        Task<Recipe?> GetByIdAsync(int recipeId);
    }
}