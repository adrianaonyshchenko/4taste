using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.Intrinsics.X86;
using System.Windows;


namespace FourTaste.WPF
{
    public partial class RegisterWindow : Window
    {
        private readonly UserService userService;

        public RegisterWindow()
        {
            InitializeComponent();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true")
                .Options;
            var context = new AppDbContext(options);
            var repository = new UserRepository(context);
            this.userService = new UserService(repository);

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

        public RegisterWindow(UserService userService)
        {
            InitializeComponent();
            this.userService = userService;
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = this.NameTextBox.Text;
                string username = this.UsernameTextBox.Text;
                string email = this.EmailTextBox.Text;
                string password = this.PasswordBox.Password;

                bool success = await this.userService.RegisterAsync(username, email, password);

                if (success)
                {
                    MessageBox.Show("Реєстрація успішна!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка реєстрації", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Перехід до вікна входу", "Увійти", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}