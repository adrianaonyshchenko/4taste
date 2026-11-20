using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface ICurrentUserService
    {
        User? CurrentUser { get; }

        int UserId { get; }

        bool IsAuthenticated { get; }

        bool IsAdmin { get; }
    }
}
