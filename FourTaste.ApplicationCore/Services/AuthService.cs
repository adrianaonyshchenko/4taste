using System;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    // Залишаємо твою прив'язку до інтерфейсу IAuthService
    public class AuthService : IAuthService
    {
        // Використовуємо IUserRepository від напарниці, бо їй потрібен метод GetByEmailAsync
        private readonly IUserRepository _userRepository;
        private readonly ILogger _logger;

        public AuthService(IUserRepository userRepository, ILogger logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // Твій метод виходу із системи
        public async Task LogoutAsync(int userId)
        {
            if (userId <= 0)
            {
                // Змінено LogWarning на Warning, щоб відповідати стандарту Serilog
                _logger.Warning("Спроба виходу з некоректним Id {UserId}", userId);
                throw new ArgumentException("Некоректний ідентифікатор користувача.", nameof(userId));
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
            {
                _logger.Warning("Спроба виходу неіснуючого користувача з Id {UserId}", userId);
                throw new ArgumentException("Користувача не знайдено.", nameof(userId));
            }

            _logger.Information("Користувач {UserId} ({Username}) вийшов із системи", user.ID, user.Username);
        }

        // Метод входу в систему твоєї напарниці
        public async Task<User> LoginAsync(string email, string password)
        {
            // ВАЛІДАЦІЯ
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.Warning("Спроба входу з пустою електронною поштою.");
                throw new ArgumentException("Електронна пошта не може бути порожньою.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.Warning("Спроба входу для пошти {Email} з пустим паролем.", email);
                throw new ArgumentException("Пароль не може бути порожнім.", nameof(password));
            }

            // Пошук за поштою
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.Warning("Користувача з поштою {Email} не знайдено в базі.", email);
                throw new UnauthorizedAccessException("Невірна електронна пошта або пароль.");
            }

            // Перевірка пароля 
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.Warning("Введено невірний пароль для користувача з поштою {Email}.", email);
                throw new UnauthorizedAccessException("Невірна електронна пошта або пароль.");
            }

            // Успішний вхід 
            _logger.Information("Користувач з поштою {Email} успішно увійшов у систему.", email);

            return user;
        }
    }
}