using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeRepository
    {
        Task AddAsync(Recipe recipe);

        Task<List<Recipe>> GetPublicRecipesAsync();

        Task<List<Recipe>> GetRecipesByUserIdAsync(int userId);
    }
}