using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IAllergyService
    {
        Task<Result> AddAllergyAsync(int userId, string allergenName);

        Task<Result<IReadOnlyList<Allergy>>> GetAllergiesByUserIdAsync(int userId);

        Task<Result<IReadOnlyList<Allergy>>> GetAllAllergiesAsync();

        Task<Result> DeleteAllergyAsync(int allergyId);
    }
}
