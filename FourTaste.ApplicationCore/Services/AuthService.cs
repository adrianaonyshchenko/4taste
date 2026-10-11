using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace FourTaste.ApplicationCore.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<Entities.User> _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IRepository<Entities.User> userRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task LogoutAsync(int userId)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("Спроба виходу з некоректним Id {UserId}", userId);
                throw new ArgumentException("Некоректний ідентифікатор користувача.", nameof(userId));
            }

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
            {
                _logger.LogWarning("Спроба виходу неіснуючого користувача з Id {UserId}", userId);
                throw new ArgumentException("Користувача не знайдено.", nameof(userId));
            }

            _logger.LogInformation("Користувач {UserId} ({Username}) вийшов із системи", user.ID, user.Username);
        }
    }
}
