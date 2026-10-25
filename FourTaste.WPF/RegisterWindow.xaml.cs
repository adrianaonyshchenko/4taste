using System;
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
        private bool isPasswordVisible;
        private bool isSyncingPassword;

        public RegisterWindow()
        {
            this.InitializeComponent();

            this.userService = App.ServiceProvider?.GetService<UserService>()
                               ?? throw new InvalidOperationException("UserService не зареєстровано в контейнері.");
        }

        public RegisterWindow(UserService userService)
        {
            this.InitializeComponent();
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.isSyncingPassword)
            {
                return;
            }

            this.isSyncingPassword = true;
            this.PasswordTextBox.Text = this.PasswordBox.Password;
            this.UpdatePasswordPlaceholder();
            this.isSyncingPassword = false;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.isSyncingPassword)
            {
                return;
            }

            this.isSyncingPassword = true;
            this.PasswordBox.Password = this.PasswordTextBox.Text;
            this.UpdatePasswordPlaceholder();
            this.isSyncingPassword = false;
        }

        private void UpdatePasswordPlaceholder()
        {
            if (this.PasswordPlaceholder != null)
            {
                string currentPassword = this.isPasswordVisible ? this.PasswordTextBox.Text : this.PasswordBox.Password;
                this.PasswordPlaceholder.Visibility = string.IsNullOrEmpty(currentPassword)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            this.isPasswordVisible = !this.isPasswordVisible;

            if (this.isPasswordVisible)
            {
                this.PasswordTextBox.Text = this.PasswordBox.Password;
                this.PasswordBox.Visibility = Visibility.Collapsed;
                this.PasswordTextBox.Visibility = Visibility.Visible;
                this.PasswordTextBox.Focus();
                this.PasswordTextBox.CaretIndex = this.PasswordTextBox.Text.Length;

                this.EyeIcon.Visibility = Visibility.Collapsed;
                this.EyeSlashIcon.Visibility = Visibility.Visible;
                this.TogglePasswordButton.ToolTip = "Приховати пароль";
            }
            else
            {
                this.PasswordBox.Password = this.PasswordTextBox.Text;
                this.PasswordTextBox.Visibility = Visibility.Collapsed;
                this.PasswordBox.Visibility = Visibility.Visible;
                this.PasswordBox.Focus();

                this.EyeIcon.Visibility = Visibility.Visible;
                this.EyeSlashIcon.Visibility = Visibility.Collapsed;
                this.TogglePasswordButton.ToolTip = "Показати пароль";
            }

            this.UpdatePasswordPlaceholder();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            this.RegisterButton.IsEnabled = false;

            try
            {
                string name = this.NameTextBox.Text?.Trim() ?? string.Empty;
                string username = this.UsernameTextBox.Text?.Trim() ?? string.Empty;
                string email = this.EmailTextBox.Text?.Trim() ?? string.Empty;
                string password = this.isPasswordVisible ? this.PasswordTextBox.Text : this.PasswordBox.Password;

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
                this.RegisterButton.IsEnabled = true;
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
