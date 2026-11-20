using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);

        Task UpdateAsync(User entity);

        Task<List<UserSummaryDto>> SearchUsersAsync(string? searchTerm, UserSearchFilter filter);

        Task<int> CountAdminsAsync();
    }
}