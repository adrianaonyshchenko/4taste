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
    }
}