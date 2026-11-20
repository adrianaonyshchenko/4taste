using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface ICurrentUserSession : ICurrentUserService
    {
        void SignIn(User user);

        void SignOut();
    }
}
