using System;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepository;
        private readonly ILogger logger;

        public AuthService(IUserRepository userRepository, ILogger? logger = null)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.logger = logger ?? Log.Logger;
        }

        public Task<Result> LogoutAsync(int userId = 0)
        {
            var currentUser = SessionContext.CurrentUser;
            SessionContext.Clear();

            if (currentUser != null)
            {
                this.logger.Information(
                    "Користувач {UserId} ({Username}) вийшов із системи (сесію очищено)",
                    currentUser.ID,
                    currentUser.Username);
            }
            else if (userId > 0)
            {
                this.logger.Information(
                    "Користувач з ID {UserId} вийшов із системи (сесію очищено)",
                    userId);
            }
            else
            {
                this.logger.Information("Сесію користувача успішно очищено (Logout)");
            }

            return Task.FromResult(Result.Success());
        }

        public async Task<Result<User>> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                this.logger.Warning("Спроба входу з пустою електронною поштою.");
                return Result<User>.Failure("Електронна пошта не може бути порожньою.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                this.logger.Warning("Спроба входу для пошти {Email} з пустим паролем.", email);
                return Result<User>.Failure("Пароль не може бути порожнім.");
            }

            var user = await this.userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                this.logger.Warning("Користувача з поштою {Email} не знайдено в базі.", email);
                return Result<User>.Failure("Невірна електронна пошта або пароль.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                this.logger.Warning("Введено невірний пароль для користувача з поштою {Email}.", email);
                return Result<User>.Failure("Невірна електронна пошта або пароль.");
            }

            this.logger.Information("Користувач з поштою {Email} успішно увійшов у систему.", email);

            return Result<User>.Success(user);
        }
    }
}