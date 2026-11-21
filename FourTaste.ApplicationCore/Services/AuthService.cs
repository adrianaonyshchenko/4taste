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
        public const string BlockedAccountError = "Ваш акаунт заблоковано адміністратором.";
        private readonly IUserRepository userRepository;
        private readonly ICurrentUserSession session;
        private readonly ILogger logger;

        public AuthService(IUserRepository userRepository, ICurrentUserSession session, ILogger? logger = null)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.logger = logger ?? Log.Logger;
        }

        public Task<Result> LogoutAsync()
        {
            var currentUser = this.session.CurrentUser;
            this.session.SignOut();

            if (currentUser is null)
            {
                this.logger.Information("Сесію користувача успішно очищено (Logout)");
                return Task.FromResult(Result.Success());
            }

            this.logger.Information(
                "Користувач {UserId} ({Username}) вийшов із системи (сесію очищено)",
                currentUser.ID,
                currentUser.Username);

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

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var user = await this.userRepository.GetByEmailAsync(email);
            this.logger.Information("Пошук користувача: {Ms} мс", sw.ElapsedMilliseconds);

            if (user == null)
            {
                this.logger.Warning("Користувача з поштою {Email} не знайдено в базі.", email);
                return Result<User>.Failure("Невірна електронна пошта або пароль.");
            }

            sw.Restart();
            bool isPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
            this.logger.Information("BCrypt: {Ms} мс", sw.ElapsedMilliseconds);

            if (!isPasswordValid)
            {
                this.logger.Warning("Введено невірний пароль для користувача з поштою {Email}.", email);
                return Result<User>.Failure("Невірна електронна пошта або пароль.");
            }

            if (user.IsBlocked)
            {
                this.logger.Warning("Спроба входу в заблокований акаунт: {Email} (ID {UserId}).", email, user.ID);
                return Result<User>.Failure(
                    string.IsNullOrWhiteSpace(user.BlockedReason)
                        ? BlockedAccountError
                        : $"{BlockedAccountError} Причина: {user.BlockedReason}");
            }

            this.session.SignIn(user);

            this.session.SignIn(user);
            this.logger.Information("Користувач з поштою {Email} успішно увійшов у систему.", email);

            return Result<User>.Success(user);
        }
    }
}