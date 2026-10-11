using System.Windows;
using Serilog;

namespace FourTaste.WPF
{
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}