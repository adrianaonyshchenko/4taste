using System;
using System.Windows;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionContext.CurrentUser is null)
            {
                MessageBox.Show("Немає активного користувача.");
                return;
            }

            var authService = App.ServiceProvider?.GetService<IAuthService>()
                ?? new AuthService(new UserRepository(new AppDbContext()), Serilog.Log.Logger);

            var result = await authService.LogoutAsync(SessionContext.CurrentUser.ID);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка виходу");
                return;
            }

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}