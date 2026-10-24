using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FourTaste.WPF
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();

            _authService = App.ServiceProvider?.GetRequiredService<AuthService>()
                           ?? throw new InvalidOperationException("ServiceProvider не ініціалізовано.");
        }

        public LoginWindow(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmailPlaceholder != null)
            {
                EmailPlaceholder.Visibility = EmailTextBox.Text.Length > 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordPlaceholder != null)
            {
                PasswordPlaceholder.Visibility = PasswordBox.Password.Length > 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;

            try
            {
                string email = EmailTextBox.Text;
                string password = PasswordBox.Password;

                var loginResult = await _authService.LoginAsync(email, password);

                if (loginResult.IsFailure)
                {
                    MessageBox.Show(loginResult.Error, "Помилка входу", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var loggedInUser = loginResult.Value!;

                if (loggedInUser != null)
                {
                    SessionContext.CurrentUser = loggedInUser;
                    MessageBox.Show("Успішний вхід у систему!", "Вітаємо", MessageBoxButton.OK, MessageBoxImage.Information);

                    var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
                    profileWindow.Show();
                    this.Close();
                }
            }
            finally
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void GoToRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = App.ServiceProvider?.GetService<RegisterWindow>() ?? new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}