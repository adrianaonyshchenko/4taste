using BCrypt.Net;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class UserService
    {
        private readonly IUserRepository userRepository;
        private readonly ILogger logger;

        public UserService(IUserRepository userRepository, ILogger? logger = null)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.logger = logger ?? Log.Logger;
        }

        public async Task<Result> RegisterAsync(string username, string email, string password, string name = "")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                this.logger.Warning("Спроба реєстрації з порожніми полями для email: {Email}", email);
                return Result.Failure("Email та пароль не можуть бути порожніми.");
            }

            if (!email.Contains('@'))
            {
                this.logger.Warning("Некоректний формат email під час реєстрації: {Email}", email);
                return Result.Failure("Некоректний формат електронної пошти.");
            }

            var existingUser = await this.userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                this.logger.Warning("Спроба зареєструвати вже зайнятий email: {Email}", email);
                return Result.Failure("Користувач з таким email вже існує.");
            }

            var newUser = new User
            {
                Name = name.Trim(),
                Username = username.Trim(),
                Email = email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            };

            await this.userRepository.AddAsync(newUser);
            this.logger.Information("Користувач {Email} успішно зареєстрований у системі", email);
            return Result.Success();
        }

        public async Task<Result<User>> GetUserByIdAsync(int userId)
        {
            var user = await this.userRepository.GetByIdAsync(userId);
            return user is null
                ? Result<User>.Failure("Користувача не знайдено.")
                : Result<User>.Success(user);
        }

        public async Task<Result> UpdateProfileAsync(int userId, string name, string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                this.logger.Warning("Спроба оновлення профілю з порожніми даними для ID: {UserId}", userId);
                return Result.Failure("Нікнейм та електронна пошта не можуть бути порожніми.");
            }

            if (!email.Contains('@'))
            {
                this.logger.Warning("Некоректний формат email під час оновлення профілю: {Email}", email);
                return Result.Failure("Некоректний формат електронної пошти.");
            }

            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                this.logger.Warning("Користувача з ID {UserId} не знайдено для оновлення", userId);
                return Result.Failure("Користувача не знайдено.");
            }

            user.Name = name.Trim();
            user.Username = username.Trim();
            user.Email = email.Trim();

            await this.userRepository.UpdateAsync(user);
            this.logger.Information("Профіль користувача з ID {UserId} успішно оновлено", userId);
            return Result.Success();
        }

        public async Task<Result> ChangePasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                this.logger.Warning("Спроба зміни пароля на порожній для користувача з ID: {UserId}", userId);
                return Result.Failure("Новий пароль не може бути порожнім.");
            }

            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                this.logger.Warning("Користувача з ID {UserId} не знайдено для зміни пароля", userId);
                return Result.Failure("Користувача не знайдено.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await this.userRepository.UpdateAsync(user);
            this.logger.Information("Пароль користувача з ID {UserId} успішно змінено", userId);
            return Result.Success();
        }

        public async Task<Result> DeleteAccountAsync(int userId)
        {
            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                this.logger.Warning("Користувача з ID {UserId} не знайдено для видалення", userId);
                return Result.Failure("Користувача не знайдено.");
            }

            await this.userRepository.DeleteAsync(user);
            this.logger.Information("Акаунт користувача з ID {UserId} успішно видалено", userId);
            return Result.Success();
        }
    }
}