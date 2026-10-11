using System;
using System.Windows;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

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

            using var context = new AppDbContext();
            var userRepository = new UserRepository(context);

            var loggerFactory = new SerilogLoggerFactory(Log.Logger);
            var logger = loggerFactory.CreateLogger<AuthService>();

            var authService = new AuthService(userRepository, Serilog.Log.Logger);

            try
            {
                await authService.LogoutAsync(SessionContext.CurrentUser.ID);

                SessionContext.CurrentUser = null;

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Помилка виходу");
            }
        }
    }
}