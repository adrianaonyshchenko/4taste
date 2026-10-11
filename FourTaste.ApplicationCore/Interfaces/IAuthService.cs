using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IAuthService
    {
        Task LogoutAsync(int userId);
    }
}
