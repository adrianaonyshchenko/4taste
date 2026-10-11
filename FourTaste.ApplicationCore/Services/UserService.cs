using BCrypt.Net;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class UserService
    {
        private readonly IUserRepository userRepository;

        public UserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public async Task<bool> RegisterAsync(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                Log.Warning("Спроба реєстрації з порожніми полями для email: {Email}", email);
                throw new ArgumentException("Email та пароль не можуть бути порожніми.");
            }

            if (!email.Contains('@'))
            {
                Log.Warning("Некоректний формат email під час реєстрації: {Email}", email);
                throw new FormatException("Некоректний формат електронної пошти.");
            }

            var existingUser = await this.userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                Log.Warning("Спроба зареєструвати вже зайнятий email: {Email}", email);
                throw new InvalidOperationException("Користувач з таким email вже існує.");
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            };

            await this.userRepository.AddAsync(newUser);
            Log.Information("Користувач {Email} успішно зареєстрований у системі", email);
            return true;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await this.userRepository.GetByIdAsync(userId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string name, string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                Log.Warning("Спроба оновлення профілю з порожніми даними для ID: {UserId}", userId);
                throw new ArgumentException("Нікнейм та електронна пошта не можуть бути порожніми.");
            }

            if (!email.Contains('@'))
            {
                Log.Warning("Некоректний формат email під час оновлення профілю: {Email}", email);
                throw new FormatException("Некоректний формат електронної пошти.");
            }

            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                Log.Warning("Користувача з ID {UserId} не знайдено для оновлення", userId);
                throw new InvalidOperationException("Користувача не знайдено.");
            }

            user.Name = name.Trim();
            user.Username = username.Trim();
            user.Email = email.Trim();

            await this.userRepository.UpdateAsync(user);
            Log.Information("Профіль користувача з ID {UserId} успішно оновлено", userId);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                Log.Warning("Спроба зміни пароля на порожній для користувача з ID: {UserId}", userId);
                throw new ArgumentException("Новий пароль не може бути порожнім.");
            }

            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                Log.Warning("Користувача з ID {UserId} не знайдено для зміни пароля", userId);
                throw new InvalidOperationException("Користувача не знайдено.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await this.userRepository.UpdateAsync(user);
            Log.Information("Пароль користувача з ID {UserId} успішно змінено", userId);
            return true;
        }

        public async Task<bool> DeleteAccountAsync(int userId)
        {
            var user = await this.userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                Log.Warning("Користувача з ID {UserId} не знайдено для видалення", userId);
                throw new InvalidOperationException("Користувача не знайдено.");
            }

            await this.userRepository.DeleteAsync(user);
            Log.Information("Акаунт користувача з ID {UserId} успішно видалено", userId);
            return true;
        }
    }
}