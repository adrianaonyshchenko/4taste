using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using Serilog;

namespace FourTaste.ApplicationCore.Services
{
    public class UserModerationService : IUserModerationService
    {
        public const string SelfActionError = "Не можна виконувати цю дію над власним акаунтом.";
        public const string AdminTargetError = "Не можна модерувати акаунт адміністратора.";
        public const string UserNotFoundError = "Користувача не знайдено.";
        public const int MaxReasonLength = 200;
        public const int MaxWarningLength = 500;

        private readonly IUserRepository userRepository;
        private readonly IUserWarningRepository warningRepository;
        private readonly ICurrentUserService currentUser;
        private readonly ILogger logger;

        public UserModerationService(
            IUserRepository userRepository,
            IUserWarningRepository warningRepository,
            ICurrentUserService currentUser,
            ILogger logger)
        {
            this.userRepository = userRepository;
            this.warningRepository = warningRepository;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Result<User>> GetUserAsync(int targetUserId)
        {
            var access = this.EnsureAdmin("перегляд акаунта", targetUserId);
            if (access.IsFailure)
            {
                return Result<User>.Failure(access.Error!);
            }

            return await this.FindUserAsync(targetUserId);
        }

        public Task<Result> BlockUserAsync(int targetUserId, string? reason)
        {
            string cleanReason = reason?.Trim() ?? string.Empty;
            if (cleanReason.Length > MaxReasonLength)
            {
                this.logger.Warning("Помилка валідації: причина блокування завелика ({Length} символів).", cleanReason.Length);
                return Task.FromResult(Result.Failure($"Причина не може бути довшою за {MaxReasonLength} символів."));
            }

            return this.ExecuteModerationAsync(targetUserId, "блокування", async target =>
            {
                if (target.IsBlocked)
                {
                    this.logger.Warning("Акаунт {TargetUserId} вже заблоковано.", targetUserId);
                    return Result.Failure("Акаунт вже заблоковано.");
                }

                target.IsBlocked = true;
                target.BlockedReason = cleanReason.Length == 0 ? null : cleanReason;
                await this.userRepository.UpdateAsync(target);

                this.logger.Information(
                    "Адміністратор {AdminId} заблокував акаунт {TargetUserId} (причина: {Reason}).",
                    this.currentUser.UserId,
                    targetUserId,
                    target.BlockedReason ?? "не вказана");

                return Result.Success();
            });
        }

        public Task<Result> UnblockUserAsync(int targetUserId)
        {
            return this.ExecuteModerationAsync(targetUserId, "розблокування", async target =>
            {
                if (!target.IsBlocked)
                {
                    this.logger.Warning("Акаунт {TargetUserId} не заблоковано, розблоковувати нічого.", targetUserId);
                    return Result.Failure("Акаунт не заблоковано.");
                }

                target.IsBlocked = false;
                target.BlockedReason = null;
                await this.userRepository.UpdateAsync(target);

                this.logger.Information("Адміністратор {AdminId} розблокував акаунт {TargetUserId}.", this.currentUser.UserId, targetUserId);
                return Result.Success();
            });
        }

        public Task<Result> SendWarningAsync(int targetUserId, string message)
        {
            string text = message?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                this.logger.Warning("Помилка валідації: порожній текст попередження.");
                return Task.FromResult(Result.Failure("Введіть текст попередження."));
            }

            if (text.Length > MaxWarningLength)
            {
                this.logger.Warning("Помилка валідації: текст попередження завеликий ({Length} символів).", text.Length);
                return Task.FromResult(Result.Failure($"Попередження не може бути довшим за {MaxWarningLength} символів."));
            }

            return this.ExecuteModerationAsync(targetUserId, "надсилання попередження", async _ =>
            {
                await this.warningRepository.AddAsync(new UserWarning
                {
                    UserId = targetUserId,
                    AdminId = this.currentUser.UserId,
                    Message = text,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                });

                this.logger.Information("Адміністратор {AdminId} надіслав попередження користувачу {TargetUserId}.", this.currentUser.UserId, targetUserId);
                return Result.Success();
            });
        }

        public Task<Result> DeleteUserAsync(int targetUserId)
        {
            return this.ExecuteModerationAsync(targetUserId, "видалення акаунта", async target =>
            {
                await this.userRepository.DeleteAsync(target);

                this.logger.Information(
                    "Адміністратор {AdminId} видалив акаунт {TargetUserId} ({Username}).",
                    this.currentUser.UserId,
                    targetUserId,
                    target.Username);

                return Result.Success();
            });
        }

        public async Task<Result<List<UserWarning>>> GetMyUnreadWarningsAsync()
        {
            if (!this.currentUser.IsAuthenticated)
            {
                return Result<List<UserWarning>>.Failure("Увійдіть в акаунт.");
            }

            var warnings = await this.warningRepository.GetUnreadAsync(this.currentUser.UserId);
            return Result<List<UserWarning>>.Success(warnings);
        }

        public async Task<Result> MarkMyWarningsReadAsync()
        {
            if (!this.currentUser.IsAuthenticated)
            {
                return Result.Failure("Увійдіть в акаунт.");
            }

            await this.warningRepository.MarkAllReadAsync(this.currentUser.UserId);
            return Result.Success();
        }

        private async Task<Result> ExecuteModerationAsync(int targetUserId, string action, Func<User, Task<Result>> moderationAction)
        {
            var access = this.EnsureAdmin(action, targetUserId);
            if (access.IsFailure)
            {
                return access;
            }

            var targetResult = await this.FindModerationTargetAsync(targetUserId, action);
            if (targetResult.IsFailure)
            {
                return Result.Failure(targetResult.Error!);
            }

            return await moderationAction(targetResult.Value!);
        }

        private Result EnsureAdmin(string action, int targetUserId)
        {
            if (this.currentUser.IsAdmin)
            {
                return Result.Success();
            }

            this.logger.Warning(
                "Користувач {UserId} без прав адміністратора намагався виконати дію '{Action}' над акаунтом {TargetUserId}.",
                this.currentUser.UserId,
                action,
                targetUserId);

            return Result.Failure(AdminService.AccessDeniedError);
        }

        private async Task<Result<User>> FindUserAsync(int targetUserId)
        {
            if (targetUserId <= 0)
            {
                this.logger.Warning("Помилка валідації: некоректний Id користувача {TargetUserId}.", targetUserId);
                return Result<User>.Failure("Некоректний ідентифікатор користувача.");
            }

            var user = await this.userRepository.GetByIdAsync(targetUserId);
            if (user is null)
            {
                this.logger.Warning("Користувача {TargetUserId} не знайдено.", targetUserId);
                return Result<User>.Failure(UserNotFoundError);
            }

            return Result<User>.Success(user);
        }

        private async Task<Result<User>> FindModerationTargetAsync(int targetUserId, string action)
        {
            if (targetUserId > 0 && targetUserId == this.currentUser.UserId)
            {
                this.logger.Warning("Адміністратор {AdminId} спробував виконати '{Action}' над власним акаунтом.", targetUserId, action);
                return Result<User>.Failure(SelfActionError);
            }

            var found = await this.FindUserAsync(targetUserId);
            if (found.IsFailure)
            {
                return found;
            }

            if (found.Value!.Role == UserRole.Admin)
            {
                this.logger.Warning(
                    "Адміністратор {AdminId} спробував виконати '{Action}' над адміністратором {TargetUserId}.",
                    this.currentUser.UserId,
                    action,
                    targetUserId);
                return Result<User>.Failure(AdminTargetError);
            }

            return found;
        }
    }
}