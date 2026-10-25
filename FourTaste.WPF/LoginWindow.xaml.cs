using System;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService;
        private bool isPasswordVisible;
        private bool isSyncingPassword;

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
            if (this.isSyncingPassword)
            {
                return;
            }

            this.isSyncingPassword = true;
            try
            {
                this.PasswordTextBox.Text = this.PasswordBox.Password;
                this.UpdatePasswordPlaceholder();
            }
            finally
            {
                this.isSyncingPassword = false;
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.isSyncingPassword)
            {
                return;
            }

            this.isSyncingPassword = true;
            try
            {
                this.PasswordBox.Password = this.PasswordTextBox.Text;
                this.UpdatePasswordPlaceholder();
            }
            finally
            {
                this.isSyncingPassword = false;
            }
        }

        private void UpdatePasswordPlaceholder()
        {
            if (this.PasswordPlaceholder != null)
            {
                string currentPassword = this.isPasswordVisible ? this.PasswordTextBox.Text : this.PasswordBox.Password;
                this.PasswordPlaceholder.Visibility = currentPassword.Length > 0
                    ? Visibility.Collapsed
                    : Visibility.Visible;
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

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;

            try
            {
                string email = EmailTextBox.Text;
                string password = this.isPasswordVisible ? this.PasswordTextBox.Text : this.PasswordBox.Password;

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
