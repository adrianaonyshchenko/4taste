using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class AdminService : IAdminService
    {
        public const string AccessDeniedError = "Недостатньо прав.";

        private readonly IUserRepository userRepository;
        private readonly ICurrentUserService currentUser;
        private readonly ILogger logger;

        public AdminService(IUserRepository userRepository, ICurrentUserService currentUser, ILogger logger)
        {
            this.userRepository = userRepository;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Result<List<UserSummaryDto>>> SearchUsersAsync(string? query, UserSearchFilter filter)
        {
            if (!this.currentUser.IsAdmin)
            {
                this.logger.Warning("Користувач {UserId} без прав адміністратора намагався переглянути список акаунтів.", this.currentUser.UserId);
                return Result<List<UserSummaryDto>>.Failure(AccessDeniedError);
            }

            var users = await this.userRepository.SearchUsersAsync(query, filter);
            this.logger.Information("Адміністратор {UserId}: пошук акаунтів '{Query}' ({Filter}), знайдено {Count}.", this.currentUser.UserId, query, filter, users.Count);
            return Result<List<UserSummaryDto>>.Success(users);
        }
    }
}
