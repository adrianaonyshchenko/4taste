using System;
using System.Windows;
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
                string name = this.NameTextBox.Text;
                string username = this.UsernameTextBox.Text;
                string email = this.EmailTextBox.Text;
                string password = this.PasswordBox.Password;

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