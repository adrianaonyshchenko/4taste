using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
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

        public async Task AddAllergyAsync(int userId, string allergenName)
        {
            if (string.IsNullOrWhiteSpace(allergenName))
            {
                this.logger.Warning(
                    "Спроба додати порожній алерген для користувача з ID {UserId}",
                    userId);
                throw new ArgumentException("Назва алергену не може бути порожньою.", nameof(allergenName));
            }

            var allergy = new Allergy
            {
                UserId = userId,
                AllergenName = allergenName.Trim(),
            };

            try
            {
                await this.allergyRepository.AddAsync(allergy);
                this.logger.Information(
                    "Алерген {AllergenName} успішно збережено для користувача з ID {UserId}",
                    allergy.AllergenName,
                    userId);
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    ex,
                    "Помилка під час збереження алергену {AllergenName} для користувача з ID {UserId}",
                    allergy.AllergenName,
                    userId);
                throw;
            }
        }

        public async Task<IReadOnlyList<Allergy>> GetAllergiesByUserIdAsync(int userId)
        {
            try
            {
                var allergies = await this.allergyRepository.ListAsync();
                var userAllergies = allergies.Where(a => a.UserId == userId).ToList();

                this.logger.Information(
                    "Отримано {Count} алергенів для користувача з ID {UserId}",
                    userAllergies.Count,
                    userId);

                return userAllergies;
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    ex,
                    "Помилка під час отримання алергенів для користувача з ID {UserId}",
                    userId);
                throw;
            }
        }

        public async Task<IReadOnlyList<Allergy>> GetAllAllergiesAsync()
        {
            try
            {
                var allergies = await this.allergyRepository.ListAsync();
                this.logger.Information(
                    "Отримано список усіх алергенів у кількості {Count}",
                    allergies.Count);
                return allergies;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Помилка під час отримання списку усіх алергенів");
                throw;
            }
        }

        public async Task DeleteAllergyAsync(int allergyId)
        {
            var existing = await this.allergyRepository.GetByIdAsync(allergyId);
            if (existing == null)
            {
                this.logger.Warning(
                    "Спроба видалити неіснуючий алерген з ID {AllergyId}",
                    allergyId);
                throw new InvalidOperationException($"Алерген з ID {allergyId} не знайдено.");
            }

            try
            {
                await this.allergyRepository.DeleteAsync(existing);
                this.logger.Information(
                    "Алерген {AllergenName} з ID {AllergyId} успішно видалено",
                    existing.AllergenName,
                    allergyId);
            }
            catch (Exception ex)
            {
                this.logger.Error(
                    ex,
                    "Помилка під час видалення алергену з ID {AllergyId}",
                    allergyId);
                throw;
            }
        }
    }
}
