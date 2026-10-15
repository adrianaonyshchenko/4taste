using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class AllergyService : IAllergyService
    {
        private readonly IRepository<Allergy> allergyRepository;
        private readonly ILogger logger;

        public AllergyService(IRepository<Allergy> allergyRepository, ILogger? logger = null)
        {
            this.allergyRepository = allergyRepository ?? throw new ArgumentNullException(nameof(allergyRepository));
            this.logger = logger ?? Log.Logger;
        }

        public async Task<Result> AddAllergyAsync(int userId, string allergenName)
        {
            if (string.IsNullOrWhiteSpace(allergenName))
            {
                this.logger.Warning(
                    "Спроба додати порожній алерген для користувача з ID {UserId}",
                    userId);
                return Result.Failure("Назва алергену не може бути порожньою.");
            }

            var allergy = new Allergy
            {
                UserId = userId,
                AllergenName = allergenName.Trim(),
            };

            await this.allergyRepository.AddAsync(allergy);
            this.logger.Information(
                "Алерген {AllergenName} успішно збережено для користувача з ID {UserId}",
                allergy.AllergenName,
                userId);
            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<Allergy>>> GetAllergiesByUserIdAsync(int userId)
        {
            var allergies = await this.allergyRepository.ListAsync();
            var userAllergies = allergies.Where(a => a.UserId == userId).ToList();

            this.logger.Information(
                "Отримано {Count} алергенів для користувача з ID {UserId}",
                userAllergies.Count,
                userId);

            return Result<IReadOnlyList<Allergy>>.Success(userAllergies);
        }

        public async Task<Result<IReadOnlyList<Allergy>>> GetAllAllergiesAsync()
        {
            var allergies = await this.allergyRepository.ListAsync();
            this.logger.Information(
                "Отримано список усіх алергенів у кількості {Count}",
                allergies.Count);
            return Result<IReadOnlyList<Allergy>>.Success(allergies);
        }

        public async Task<Result> DeleteAllergyAsync(int allergyId)
        {
            var existing = await this.allergyRepository.GetByIdAsync(allergyId);
            if (existing == null)
            {
                this.logger.Warning(
                    "Спроба видалити неіснуючий алерген з ID {AllergyId}",
                    allergyId);
                return Result.Failure($"Алерген з ID {allergyId} не знайдено.");
            }

            await this.allergyRepository.DeleteAsync(existing);
            this.logger.Information(
                "Алерген {AllergenName} з ID {AllergyId} успішно видалено",
                existing.AllergenName,
                allergyId);
            return Result.Success();
        }
    }
}
