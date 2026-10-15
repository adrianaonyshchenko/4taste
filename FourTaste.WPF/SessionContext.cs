using System;
using System.Collections.Generic;
using System.Text;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.WPF
{
    public static class SessionContext
    {
        public static User? CurrentUser { get; set; }
    }
}
