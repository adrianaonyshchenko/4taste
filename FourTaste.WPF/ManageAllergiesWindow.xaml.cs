using System;
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
        private readonly int currentUserId = 1;

        public ManageAllergiesWindow()
            : this(ResolveAllergyService(), ResolveUserService())
        {
        }

        public ManageAllergiesWindow(IAllergyService allergyService)
            : this(allergyService, ResolveUserService())
        {
        }

        public ManageAllergiesWindow(IAllergyService allergyService, UserService userService)
        {
            this.InitializeComponent();
            this.allergyService = allergyService ?? throw new ArgumentNullException(nameof(allergyService));
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.LoadUserProfileAsync();
            await this.LoadAllergiesAsync();
        }

        private async Task LoadUserProfileAsync()
        {
            try
            {
                var user = await this.userService.GetUserByIdAsync(this.currentUserId);
                if (user != null)
                {
                    this.ProfileNameTextBox.Text = user.Name;
                    this.ProfileUsernameTextBox.Text = user.Username;
                    this.ProfileEmailTextBox.Text = user.Email;
                    this.DisplayNameTextBlock.Text = string.IsNullOrWhiteSpace(user.Name) ? "Ім'я" : user.Name;
                    this.DisplayUsernameTextBlock.Text = string.IsNullOrWhiteSpace(user.Username) ? "Нікнейм" : user.Username;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка завантаження профілю користувача з ID {UserId}", this.currentUserId);
            }
        }

        private async Task LoadAllergiesAsync()
        {
            try
            {
                var allergies = await this.allergyService.GetAllergiesByUserIdAsync(this.currentUserId);
                this.AllergiesListBox.ItemsSource = allergies;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не вдалося завантажити список алергенів: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void SaveAllergyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string allergenName = this.AllergenTextBox.Text;
                await this.allergyService.AddAllergyAsync(this.currentUserId, allergenName);

                this.AllergenTextBox.Clear();
                await this.LoadAllergiesAsync();

                MessageBox.Show(
                    "Алерген успішно збережено!",
                    "Успіх",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Помилка валідації",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка збереження: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteAllergyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.AllergiesListBox.SelectedItem is not Allergy selectedAllergy)
            {
                MessageBox.Show(
                    "Будь ласка, оберіть алерген зі списку для видалення.",
                    "Увага",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                await this.allergyService.DeleteAllergyAsync(selectedAllergy.Id);
                await this.LoadAllergiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка видалення: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void UpdateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = this.ProfileNameTextBox.Text;
                string username = this.ProfileUsernameTextBox.Text;
                string email = this.ProfileEmailTextBox.Text;

                await this.userService.UpdateProfileAsync(this.currentUserId, name, username, email);

                this.DisplayNameTextBlock.Text = string.IsNullOrWhiteSpace(name) ? "Ім'я" : name.Trim();
                this.DisplayUsernameTextBlock.Text = username.Trim();

                MessageBox.Show(
                    "Особисті дані успішно оновлено!",
                    "Успіх",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка оновлення профілю: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPassword = this.NewPasswordBox.Password;
                await this.userService.ChangePasswordAsync(this.currentUserId, newPassword);
                this.NewPasswordBox.Clear();

                MessageBox.Show(
                    "Пароль успішно змінено!",
                    "Успіх",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка зміни пароля: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Ви впевнені, що хочете видалити свій обліковий запис?",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await this.userService.DeleteAccountAsync(this.currentUserId);
                MessageBox.Show(
                    "Обліковий запис видалено.",
                    "Успіх",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var registerWindow = App.ServiceProvider?.GetService<RegisterWindow>() ?? new RegisterWindow();
                registerWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Помилка видалення профілю: {ex.Message}",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач з ID {UserId} вийшов із системи", this.currentUserId);
            var registerWindow = App.ServiceProvider?.GetService<RegisterWindow>() ?? new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}
