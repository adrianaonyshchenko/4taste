using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeCategoryRepository
    {
        Task<List<RecipeCategory>> GetAllAsync();

        Task<RecipeCategory?> GetByIdAsync(int id);

        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

        Task<int> CountRecipesByCategoryNameAsync(string categoryName);

        Task AddAsync(RecipeCategory category);

        Task UpdateAsync(RecipeCategory category);

        Task RenameCategoryInRecipesAsync(string oldName, string newName);

        Task DeleteAsync(int id);
    }
}