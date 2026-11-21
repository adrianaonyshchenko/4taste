using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IUserModerationService
    {
        Task<Result<User>> GetUserAsync(int targetUserId);

        Task<Result> BlockUserAsync(int targetUserId, string? reason);

        Task<Result> UnblockUserAsync(int targetUserId);

        Task<Result> SendWarningAsync(int targetUserId, string message);

        Task<Result> DeleteUserAsync(int targetUserId);

        Task<Result<List<UserWarning>>> GetMyUnreadWarningsAsync();

        Task<Result> MarkMyWarningsReadAsync();
    }
}