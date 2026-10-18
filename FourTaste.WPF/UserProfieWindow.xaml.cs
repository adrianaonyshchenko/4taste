using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class UserProfileWindow : Window
    {
        private readonly MealPlanService _mealPlanService;
        private readonly AuthService _authService;
        public UserProfileWindow()
        {
            InitializeComponent();

            _mealPlanService = App.ServiceProvider?.GetRequiredService<MealPlanService>()!;
            _authService = App.ServiceProvider?.GetRequiredService<AuthService>()!;

            if (SessionContext.CurrentUser != null)
            {
                UserNameTextBlock.Text = SessionContext.CurrentUser.Email;
            }

            MenuCalendar.SelectedDate = DateTime.Today;
        }

        public UserProfileWindow(MealPlanService mealPlanService, AuthService authService)
        {
            InitializeComponent();
            _mealPlanService = mealPlanService;
            _authService = authService;

            if (SessionContext.CurrentUser != null)
            {
                UserNameTextBlock.Text = SessionContext.CurrentUser.Email;
            }

            MenuCalendar.SelectedDate = DateTime.Today;
        }

        private async void MenuCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MenuCalendar.SelectedDate.Value;
                SelectedDateTextBlock.Text = selectedDate.ToString("d MMMM");

                if (SessionContext.CurrentUser != null)
                {
                    var plans = await _mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
                    PlannedRecipesListBox.ItemsSource = plans;
                }
            }
        }

        private async void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!MenuCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, оберіть дату в календарі.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SessionContext.CurrentUser == null)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var recipeTextBox = FindName("RecipeTextBox") as TextBox;
            string recipeName = recipeTextBox?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(recipeName))
            {
                MessageBox.Show("Будь ласка, введіть назву страви.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime selectedDate = MenuCalendar.SelectedDate.Value;

            var result = await _mealPlanService.AddCustomRecipeToPlanAsync(SessionContext.CurrentUser.ID, recipeName, selectedDate);

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

            var plans = await _mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
            PlannedRecipesListBox.ItemsSource = plans;
        }

        // Обробник для нової кнопки видалення страви
        private async void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!MenuCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, оберіть дату в календарі.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SessionContext.CurrentUser == null)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (PlannedRecipesListBox.SelectedItem is not FourTaste.ApplicationCore.Entities.MealPlan selectedMealPlan)
            {
                MessageBox.Show("Будь ласка, виберіть страву зі списку для видалення.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show($"Ви дійсно бажаєте видалити страву '{selectedMealPlan.RecipeName}'?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            // Викликаємо сервіс для видалення страви (використовуємо ID запису плану або метод видалення за ID)
            var result = await _mealPlanService.DeleteMealPlanAsync(selectedMealPlan.Id, SessionContext.CurrentUser.ID);

            if (result.IsFailure)
            {
                MessageBox.Show($"Не вдалося видалити страву: {result.Error}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Страву успішно видалено з меню!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

            // Оновлюємо список страв для вибраної дати
            DateTime selectedDate = MenuCalendar.SelectedDate.Value;
            var plans = await _mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
            PlannedRecipesListBox.ItemsSource = plans;
        }

        private void SaveAllergensButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач натиснув 'Зберегти алергени'");
            MessageBox.Show("Алергени успішно збережено!", "Профіль", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MyProductsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Відкриття розділу 'Мої продукти'");
            MessageBox.Show("Перехід до розділу 'Мої продукти'.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Ви дійсно бажаєте видалити свій профіль?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                Log.Warning("Користувач ініціював видалення профілю.");
                MessageBox.Show("Профіль успішно видалено.", "Видалення", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }

        private void EditDataButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Натиснуто кнопку 'Змінити дані'");
            MessageBox.Show("Форма редагування персональних даних.", "Профіль", MessageBoxButton.OK, MessageBoxImage.Information);
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
            LogoutButton.IsEnabled = false;

            try
            {
                var result = await _authService.LogoutAsync();

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
                LogoutButton.IsEnabled = true;
            }
        }
        private async void RecipeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FourTaste.ApplicationCore.Entities.MealPlan mealPlan)
            {
                if (SessionContext.CurrentUser == null)
                {
                    MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    checkBox.IsChecked = mealPlan.IsCooked;
                    return;
                }

                bool newStatus = checkBox.IsChecked ?? false;

                var result = await _mealPlanService.UpdateMealPlanStatusAsync(mealPlan.Id, SessionContext.CurrentUser.ID, newStatus);

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