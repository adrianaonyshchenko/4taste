using System;
using System.Linq;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Xunit;

namespace FourTaste.Tests
{
    public class CurrentUserServiceTests
    {
        [Fact]
        public void NewSession_ShouldBeAnonymous()
        {
            var service = new CurrentUserService();

            Assert.Null(service.CurrentUser);
            Assert.Equal(0, service.UserId);
            Assert.False(service.IsAuthenticated);
        }

        [Fact]
        public void SignIn_ShouldExposeUser()
        {
            var service = new CurrentUserService();
            var user = new User { ID = 7, Username = "sofia" };

            service.SignIn(user);

            Assert.Same(user, service.CurrentUser);
            Assert.Equal(7, service.UserId);
            Assert.True(service.IsAuthenticated);
        }

        [Fact]
        public void SignOut_ShouldClearUser()
        {
            var service = new CurrentUserService();
            service.SignIn(new User { ID = 7 });

            service.SignOut();

            Assert.Null(service.CurrentUser);
            Assert.False(service.IsAuthenticated);
        }

        [Fact]
        public void SignIn_ShouldThrow_WhenUserIsNull()
        {
            var service = new CurrentUserService();

            Assert.Throws<ArgumentNullException>(() => service.SignIn(null!));
        }

        [Fact]
        public void ICurrentUserService_ShouldBeReadOnly()
        {
            var type = typeof(ICurrentUserService);

            Assert.All(type.GetProperties(), p => Assert.False(p.CanWrite, $"{p.Name} не повинна мати сеттер"));
            Assert.DoesNotContain(type.GetMethods(), m => !m.IsSpecialName);
        }
    }
}
