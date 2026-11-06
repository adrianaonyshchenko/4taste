using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class UserProfileWindow : Window
    {
        private readonly MealPlanService mealPlanService;
        private readonly AuthService authService;
        private readonly IAllergyService allergyService;
        private readonly UserService userService;
        private readonly IRecipeService _recipeService;
        private readonly IFridgeService _fridgeService;
        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public UserProfileWindow()
            : this(
                App.ServiceProvider?.GetRequiredService<MealPlanService>()!,
                App.ServiceProvider?.GetRequiredService<AuthService>()!,
                App.ServiceProvider?.GetRequiredService<IAllergyService>()!,
                App.ServiceProvider?.GetRequiredService<UserService>()!,
                App.ServiceProvider?.GetRequiredService<IRecipeService>()!,
                App.ServiceProvider?.GetRequiredService<IFridgeService>()!)
        {
        }

        public UserProfileWindow(
            MealPlanService mealPlanService,
            AuthService authService,
            IAllergyService? allergyService = null,
            UserService? userService = null,
            IRecipeService? recipeService = null,
            IFridgeService? fridgeService = null)
        {
            this.InitializeComponent();

            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.allergyService = allergyService ?? App.ServiceProvider?.GetRequiredService<IAllergyService>()!;
            this.userService = userService ?? App.ServiceProvider?.GetRequiredService<UserService>()!;
            this._recipeService = recipeService ?? App.ServiceProvider?.GetRequiredService<IRecipeService>()!;
            this._fridgeService = fridgeService ?? App.ServiceProvider?.GetRequiredService<IFridgeService>()!;

            this.UpdateUserHeader();
            _ = this.LoadAllergiesAsync();

            this.Loaded += async (s, e) =>
            {
                if (SessionContext.CurrentUser != null && (SessionContext.CurrentUser.Avatar == null || SessionContext.CurrentUser.Avatar.Length == 0))
                {
                    int uid = SessionContext.CurrentUser.ID;
                    var dbBytes = await Task.Run(() => AvatarService.LoadAvatarBytes(uid));
                    if (dbBytes != null && dbBytes.Length > 0 && SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == uid)
                    {
                        SessionContext.CurrentUser.Avatar = dbBytes;
                        this.UpdateUserHeader();
                    }
                }
            };
        }

        public void UpdateUserHeader()
        {
            if (SessionContext.CurrentUser != null)
            {
                this.UserNameTextBlock.Text = string.IsNullOrWhiteSpace(SessionContext.CurrentUser.Name)
                    ? (string.IsNullOrWhiteSpace(SessionContext.CurrentUser.Email) ? "Користувач" : SessionContext.CurrentUser.Email)
                    : SessionContext.CurrentUser.Name;

                string username = string.IsNullOrWhiteSpace(SessionContext.CurrentUser.Username)
                    ? "Нікнейм"
                    : SessionContext.CurrentUser.Username;

                this.UserNicknameTextBlock.Text = username.StartsWith('@') ? username : $"@{username}";

                var avatarImage = AvatarService.LoadAvatar(SessionContext.CurrentUser.ID);
                if (avatarImage != null)
                {
                    this.UserAvatarImageBrush.ImageSource = avatarImage;
                    this.CustomAvatarBorder.Visibility = Visibility.Visible;
                    this.DefaultAvatarBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.UserAvatarImageBrush.ImageSource = null;
                    this.CustomAvatarBorder.Visibility = Visibility.Collapsed;
                    this.DefaultAvatarBorder.Visibility = Visibility.Visible;
                }
            }
        }

        private async Task LoadAllergiesAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                return;
            }

            var result = await this.allergyService.GetAllergiesByUserIdAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження алергенів", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.AllergiesListBox.ItemsSource = result.Value;
        }

        private async void SaveAllergensButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string allergenName = this.AllergenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(allergenName))
            {
                MessageBox.Show("Будь ласка, введіть назву алергену.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

        private void AllergenTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.SaveAllergensButton_Click(this.SaveAllergensButton, new RoutedEventArgs());
            }
        }

        private async void DeleteAllergyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.AllergiesListBox.SelectedItem is not Allergy selectedAllergy)
            {
                MessageBox.Show("Будь ласка, оберіть алерген зі списку для видалення.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Ви дійсно бажаєте видалити алерген '{selectedAllergy.AllergenName}'?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.allergyService.DeleteAllergyAsync(selectedAllergy.Id);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadAllergiesAsync();
            MessageBox.Show("Алерген успішно видалено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenMealPlanButton_Click(object sender, RoutedEventArgs e)
        {
            var mealPlanWindow = new MealPlanWindow(this.mealPlanService);
            mealPlanWindow.ShowDialog();
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show("Ви дійсно бажаєте видалити свій профіль? Цю дію неможливо буде скасувати.", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.userService.DeleteAccountAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення профілю", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Профіль успішно видалено.", "Видалення", MessageBoxButton.OK, MessageBoxImage.Information);
            SessionContext.Clear();

            var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void EditDataButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Натиснуто кнопку 'Змінити дані'");
            var editWindow = App.ServiceProvider?.GetService<EditProfileWindow>() ?? new EditProfileWindow();
            editWindow.Show();
            this.Close();
        }

        private void CookingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач переходить до історії готування");
            var historyWindow = App.ServiceProvider?.GetService<CookingHistoryWindow>() ?? new CookingHistoryWindow();
            historyWindow.Show();
            this.Close();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.LogoutButton.IsEnabled = false;

            try
            {
                var result = await this.authService.LogoutAsync();

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка виходу", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            finally
            {
                this.LogoutButton.IsEnabled = true;
            }
        }

        private void MyRecipesButton_Click(object sender, RoutedEventArgs e)
        {
            var myRecipesWindow = new MyRecipesWindow(this._recipeService, this.CurrentUserId);
            myRecipesWindow.Show();
            this.Close();
        }

        private void MyFridgeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач відкриває вікно 'Мої продукти' (Холодильник)");
            var fridgeWindow = new FridgeWindow(this._fridgeService, this.CurrentUserId);
            fridgeWindow.ShowDialog();
        }

        private void OpenCuisinesButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Відкриття вікна 'Кухні світу'");
            var cuisinesWindow = new UserCuisinesWindow(this._recipeService, this.CurrentUserId);
            cuisinesWindow.ShowDialog();
        }

        private void CuisinesBlock_Click(object sender, MouseButtonEventArgs e)
        {
            OpenCuisinesButton_Click(sender, e);
        }
    }
}
