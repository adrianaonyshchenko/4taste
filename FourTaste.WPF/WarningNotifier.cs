using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public static class WarningNotifier
    {
        private const int MaxShown = 5;

        public static async Task ShowUnreadAsync()
        {
            var service = App.ServiceProvider?.GetService<IUserModerationService>();
            if (service is null)
            {
                return;
            }

            var result = await service.GetMyUnreadWarningsAsync();
            if (result.IsFailure || result.Value is not { Count: > 0 } warnings)
            {
                return;
            }

            string text = string.Join(
                "\n\n",
                warnings.Take(MaxShown).Select(w => $"{w.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}\n{w.Message}"));

            if (warnings.Count > MaxShown)
            {
                text += $"\n\n…і ще {warnings.Count - MaxShown}.";
            }

            MessageBox.Show(text, "Попередження від адміністратора", MessageBoxButton.OK, MessageBoxImage.Warning);
            await service.MarkMyWarningsReadAsync();
        }
    }
}