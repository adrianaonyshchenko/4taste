using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class RegisterWindow : Window
    {
        private readonly UserService userService;

        public RegisterWindow()
        {
            InitializeComponent();

            this.userService = App.ServiceProvider?.GetService<UserService>()
                               ?? throw new InvalidOperationException("UserService не зареєстровано в контейнері.");
        }
        public RegisterWindow(UserService userService)
        {
            InitializeComponent();
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordPlaceholder != null)
            {
                PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterButton.IsEnabled = false;

            try
            {
                string name = this.NameTextBox.Text?.Trim() ?? string.Empty;
                string username = this.UsernameTextBox.Text?.Trim() ?? string.Empty;
                string email = this.EmailTextBox.Text?.Trim() ?? string.Empty;
                string password = this.PasswordBox.Password;

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Будь ласка, заповніть усі поля.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string usernamePattern = @"^[a-zA-Z0-9_-]+$";
                if (!Regex.IsMatch(username, usernamePattern))
                {
                    MessageBox.Show("Нікнейм має складатися лише з англійських літер, цифр та символів '_' або '-'.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!Regex.IsMatch(email, emailPattern))
                {
                    MessageBox.Show("Електронна пошта має бути написана англійськими літерами та мати коректний формат (наприклад, example@gmail.com).", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (password.Length < 6)
                {
                    MessageBox.Show("Пароль має містити щонайменше 6 символів.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string passwordPattern = @"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]*$";
                if (!Regex.IsMatch(password, passwordPattern))
                {
                    MessageBox.Show("Пароль може містити лише англійські літери, цифри та спеціальні символи.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = await this.userService.RegisterAsync(username, email, password, name);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка реєстрації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Реєстрація успішна! Тепер увійдіть у систему.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            finally
            {
                RegisterButton.IsEnabled = true;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}