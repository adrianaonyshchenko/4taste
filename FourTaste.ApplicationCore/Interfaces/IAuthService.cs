using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IAuthService
    {
        Task<Result> LogoutAsync(int userId = 0);

        Task<Result<User>> LoginAsync(string email, string password);
    }
}
