using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IAdminService
    {
        Task<Result<List<UserSummaryDto>>> SearchUsersAsync(string? query, UserSearchFilter filter);
    }
}
