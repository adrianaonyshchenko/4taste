using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IFavoriteRepository
    {
        Task AddFavoriteAsync(int userId, int recipeId);
        Task RemoveFavoriteAsync(int userId, int recipeId);
        Task<bool> IsFavoriteAsync(int userId, int recipeId);
        Task<List<Recipe>> GetFavoriteRecipesByUserIdAsync(int userId);
    }
}
