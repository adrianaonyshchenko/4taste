using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface ISubstituteService
    {
        Task<Result<IReadOnlyList<Ingredient>>> GetConflictingIngredientsAsync(int userId, int recipeId);

        Task<Result<SafeSubstitutesResponse>> GetSafeSubstitutesAsync(int userId, int recipeId, int ingredientId);

        Task<Result<AppliedSubstitutionDto>> ApplySubstitutionAsync(int userId, int recipeId, int ingredientId, int substituteId);

        Task<Result<IReadOnlyList<AppliedSubstitutionDto>>> GetUserSubstitutionsAsync(int userId, int recipeId);

        Task<Result> RemoveSubstitutionAsync(int userId, int recipeId, int ingredientId);
    }
}

