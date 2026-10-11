using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;

namespace FourTaste.WPF
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true")
                .Options;

            var context = new AppDbContext(options);
            var repository = new UserRepository(context);
            var logger = Log.Logger;

            _authService = new AuthService(repository, logger);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = EmailTextBox.Text;
                string password = PasswordBox.Password;

                var loggedInUser = await _authService.LoginAsync(email, password);

                if (loggedInUser != null)
                {
                    SessionContext.CurrentUser = loggedInUser;

                    MessageBox.Show("Успішний вхід у систему!", "Вітаємо", MessageBoxButton.OK, MessageBoxImage.Information);

                    var profileWindow = App.ServiceProvider?.GetService<ManageAllergiesWindow>() ?? new ManageAllergiesWindow();
                    profileWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка входу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoToRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}