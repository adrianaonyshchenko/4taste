using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IUserWarningRepository
    {
        Task AddAsync(UserWarning warning);
        Task<List<UserWarning>> GetUnreadAsync(int userId);
        Task MarkAllReadAsync(int userId);
    }
}