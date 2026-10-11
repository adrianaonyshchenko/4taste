using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Services
{
    public static class SessionContext
    {
        public static User? CurrentUser { get; set; }

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}