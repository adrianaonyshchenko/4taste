using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Moq;

namespace FourTaste.Tests
{
    public static class CurrentUserMock
    {
        public static Mock<ICurrentUserService> Authenticated(int userId, bool isAdmin = false)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(c => c.CurrentUser).Returns(new User { ID = userId, Username = $"user{userId}", Role = isAdmin ? UserRole.Admin : UserRole.User });
            mock.Setup(c => c.UserId).Returns(userId);
            mock.Setup(c => c.IsAuthenticated).Returns(true);
            mock.Setup(c => c.IsAdmin).Returns(isAdmin);
            return mock;
        }

        public static Mock<ICurrentUserService> Anonymous()
        {
            var mock = new Mock<ICurrentUserService>();
            mock.Setup(c => c.CurrentUser).Returns((User?)null);
            mock.Setup(c => c.UserId).Returns(0);
            mock.Setup(c => c.IsAuthenticated).Returns(false);
            return mock;
        }
    }
}
