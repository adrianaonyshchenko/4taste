using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public UserProfileWindow()
            : this(
                App.ServiceProvider?.GetRequiredService<MealPlanService>()!,
                App.ServiceProvider?.GetRequiredService<AuthService>()!,
                App.ServiceProvider?.GetRequiredService<IAllergyService>()!,
                App.ServiceProvider?.GetRequiredService<UserService>()!)
        {
        }

        public UserProfileWindow(
            MealPlanService mealPlanService,
            AuthService authService,
            IAllergyService? allergyService = null,
            UserService? userService = null)
        {
            this.InitializeComponent();

            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.allergyService = allergyService ?? App.ServiceProvider?.GetRequiredService<IAllergyService>()!;
            this.userService = userService ?? App.ServiceProvider?.GetRequiredService<UserService>()!;

            this.UpdateUserHeader();
            this.MenuCalendar.SelectedDate = DateTime.Today;
            this.Loaded += this.UserProfileWindow_Loaded;
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

        private async void UserProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateUserHeader();
            await this.LoadAllergiesAsync();
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

        private async void MenuCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MenuCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;
                this.SelectedDateTextBlock.Text = selectedDate.ToString("d MMMM");

                if (SessionContext.CurrentUser != null)
                {
                    var plans = await this.mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
                    this.PlannedRecipesListBox.ItemsSource = plans;
                }
            }
        }

        private async void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.MenuCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, оберіть дату в календарі.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SessionContext.CurrentUser == null)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var recipeTextBox = this.FindName("RecipeTextBox") as TextBox;
            string recipeName = recipeTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(recipeName))
            {
                MessageBox.Show("Будь ласка, введіть назву страви.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;

            var result = await this.mealPlanService.AddCustomRecipeToPlanAsync(SessionContext.CurrentUser.ID, recipeName, selectedDate);

            if (result.IsFailure)
            {
                MessageBox.Show($"Помилка: {result.Error}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Страву '{recipeName}' успішно збережено на {selectedDate:d MMMM}!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            if (recipeTextBox != null)
            {
                recipeTextBox.Text = string.Empty;
            }

            var plans = await this.mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
            this.PlannedRecipesListBox.ItemsSource = plans;
        }

        private async void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.MenuCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, оберіть дату в календарі.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SessionContext.CurrentUser == null)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.PlannedRecipesListBox.SelectedItem is not MealPlan selectedMealPlan)
            {
                MessageBox.Show("Будь ласка, виберіть страву зі списку для видалення.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show($"Ви дійсно бажаєте видалити страву '{selectedMealPlan.RecipeName}'?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.mealPlanService.DeleteMealPlanAsync(selectedMealPlan.Id, SessionContext.CurrentUser.ID);

            if (result.IsFailure)
            {
                MessageBox.Show($"Не вдалося видалити страву: {result.Error}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Страву успішно видалено з меню!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;
            var plans = await this.mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
            this.PlannedRecipesListBox.ItemsSource = plans;
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

        private void MyProductsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Відкриття розділу 'Мої продукти'");
            MessageBox.Show("Перехід до розділу 'Мої продукти'.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void List1Button_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Відкрито перший список покупок");
            MessageBox.Show("Відкрито 'Список 1'.", "Списки", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void List2Button_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Відкрито другий список покупок");
            MessageBox.Show("Відкрито 'Список 2'.", "Списки", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void RecipeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is MealPlan mealPlan)
            {
                if (SessionContext.CurrentUser == null)
                {
                    MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    checkBox.IsChecked = mealPlan.IsCooked;
                    return;
                }

                bool newStatus = checkBox.IsChecked ?? false;

                var result = await this.mealPlanService.UpdateMealPlanStatusAsync(mealPlan.Id, SessionContext.CurrentUser.ID, newStatus);

                if (result.IsFailure)
                {
                    MessageBox.Show($"Не вдалося оновити статус: {result.Error}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    checkBox.IsChecked = mealPlan.IsCooked;
                    return;
                }

                mealPlan.IsCooked = newStatus;
            }
        }
    }
}