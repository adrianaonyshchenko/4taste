using System;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;

namespace FourTaste.ApplicationCore.Services
{
    public sealed class CurrentUserService : ICurrentUserSession
    {
        private User? currentUser;

        public User? CurrentUser => this.currentUser;

        public int UserId => this.currentUser?.ID ?? 0;

        public bool IsAuthenticated => this.UserId > 0;

        public void SignIn(User user)
        {
            this.currentUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        public void SignOut()
        {
            this.currentUser = null;
        }
    }
}
