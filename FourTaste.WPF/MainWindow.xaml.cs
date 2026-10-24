using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;

namespace FourTaste.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IAuthService? _authService;
        public MainWindow()
        {
            InitializeComponent();
            _authService = App.ServiceProvider?.GetService<IAuthService>();
        }
        public MainWindow(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionContext.CurrentUser is null)
            {
                MessageBox.Show("Немає активного користувача.");
                return;
            }

            var authService = _authService ?? App.ServiceProvider?.GetService<IAuthService>();

            if (authService != null)
            {
                var result = await authService.LogoutAsync(SessionContext.CurrentUser.ID);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка виходу");
                    return;
                }
            }

            SessionContext.CurrentUser = null;

            var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}