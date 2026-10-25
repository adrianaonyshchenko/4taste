using System;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class ManageAllergiesWindow : Window
    {
        private readonly IAllergyService allergyService;
        private readonly UserService userService;
        private readonly IAuthService authService;
        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public ManageAllergiesWindow()
            : this(ResolveAllergyService(), ResolveUserService(), ResolveAuthService())
        {
        }

        public ManageAllergiesWindow(IAllergyService allergyService, UserService userService, IAuthService authService)
        {
            this.InitializeComponent();
            this.allergyService = allergyService ?? throw new ArgumentNullException(nameof(allergyService));
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        private static IAllergyService ResolveAllergyService()
        {
            if (App.ServiceProvider != null)
            {
                var resolved = App.ServiceProvider.GetService<IAllergyService>();
                if (resolved != null)
                {
                    return resolved;
                }
            }
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true")
                .Options;
            var context = new AppDbContext(options);
            var repository = new AllergyRepository(context);
            return new AllergyService(repository);
        }

        private static UserService ResolveUserService()
        {
            if (App.ServiceProvider != null)
            {
                var resolved = App.ServiceProvider.GetService<UserService>();
                if (resolved != null)
                {
                    return resolved;
                }
            }
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true")
                .Options;
            var context = new AppDbContext(options);
            var repository = new UserRepository(context);
            return new UserService(repository);
        }

        private static IAuthService ResolveAuthService()
        {
            if (App.ServiceProvider != null)
            {
                var resolved = App.ServiceProvider.GetService<IAuthService>();
                if (resolved != null)
                {
                    return resolved;
                }
            }
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=ep-mute-sea-b5ks3hrm-pooler.c-7.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_a8u2btDTpHiX;Ssl Mode=Require;Trust Server Certificate=true")
                .Options;
            var context = new AppDbContext(options);
            var repository = new UserRepository(context);
            return new AuthService(repository, Log.Logger);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.LoadUserProfileAsync();
            await this.LoadAllergiesAsync();
        }

        private async Task LoadUserProfileAsync()
        {
            var result = await this.userService.GetUserByIdAsync(this.CurrentUserId);
            if (result.IsSuccess && result.Value is { } user)
            {
                this.ProfileNameTextBox.Text = user.Name;
                this.ProfileUsernameTextBox.Text = user.Username;
                this.ProfileEmailTextBox.Text = user.Email;
                this.DisplayNameTextBlock.Text = string.IsNullOrWhiteSpace(user.Name) ? "Ім'я" : user.Name;
                this.DisplayUsernameTextBlock.Text = string.IsNullOrWhiteSpace(user.Username) ? "Нікнейм" : user.Username;
            }
        }

        private async Task LoadAllergiesAsync()
        {
            var result = await this.allergyService.GetAllergiesByUserIdAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.AllergiesListBox.ItemsSource = result.Value;
        }

        private async void SaveAllergyButton_Click(object sender, RoutedEventArgs e)
        {
            string allergenName = this.AllergenTextBox.Text;
            var result = await this.allergyService.AddAllergyAsync(this.CurrentUserId, allergenName);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.AllergenTextBox.Clear();
            await this.LoadAllergiesAsync();
            MessageBox.Show("Алерген успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteAllergyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.AllergiesListBox.SelectedItem is not Allergy selectedAllergy)
            {
                MessageBox.Show("Будь ласка, оберіть алерген зі списку для видалення.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = await this.allergyService.DeleteAllergyAsync(selectedAllergy.Id);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadAllergiesAsync();
        }

        private async void UpdateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string name = this.ProfileNameTextBox.Text;
            string username = this.ProfileUsernameTextBox.Text;
            string email = this.ProfileEmailTextBox.Text;
            var result = await this.userService.UpdateProfileAsync(this.CurrentUserId, name, username, email);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка оновлення профілю", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DisplayNameTextBlock.Text = string.IsNullOrWhiteSpace(name) ? "Ім'я" : name.Trim();
            this.DisplayUsernameTextBlock.Text = username.Trim();
            MessageBox.Show("Особисті дані успішно оновлено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = this.NewPasswordBox.Password;
            var result = await this.userService.ChangePasswordAsync(this.CurrentUserId, newPassword);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка зміни пароля", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.NewPasswordBox.Clear();
            MessageBox.Show("Пароль успішно змінено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Ви впевнені, що хочете видалити свій обліковий запис?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.userService.DeleteAccountAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення профілю", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Обліковий запис видалено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            SessionContext.Clear();

            var registerWindow = App.ServiceProvider?.GetService<RegisterWindow>() ?? new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await this.authService.LogoutAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка при виході", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var registerWindow = App.ServiceProvider?.GetService<RegisterWindow>() ?? new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}