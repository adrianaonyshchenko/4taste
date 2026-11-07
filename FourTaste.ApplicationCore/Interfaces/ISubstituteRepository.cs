using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface ISubstituteRepository
    {
        Task<List<AllergenCatalogItem>> GetAllergenCatalogAsync();

        Task<List<IngredientSubstitute>> GetAllSubstitutesAsync();

        Task<IngredientSubstitute?> GetSubstituteByIdAsync(int substituteId);

        Task<List<UserRecipeSubstitution>> GetUserRecipeSubstitutionsAsync(int userId, int recipeId);

        Task<UserRecipeSubstitution?> GetUserRecipeSubstitutionAsync(int userId, int recipeId, int ingredientId);

        Task SaveUserRecipeSubstitutionAsync(UserRecipeSubstitution substitution);

        Task RemoveUserRecipeSubstitutionAsync(int userId, int recipeId, int ingredientId);
    }
}

