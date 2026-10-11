using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IAllergyService
    {
        Task AddAllergyAsync(int userId, string allergenName);

        Task<IReadOnlyList<Allergy>> GetAllergiesByUserIdAsync(int userId);

        Task<IReadOnlyList<Allergy>> GetAllAllergiesAsync();

        Task DeleteAllergyAsync(int allergyId);
    }
}
