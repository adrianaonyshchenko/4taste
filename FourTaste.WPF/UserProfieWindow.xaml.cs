using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class UserProfileWindow : Window
    {
        private readonly MealPlanService _mealPlanService;
        public UserProfileWindow()
        {
            InitializeComponent();

            _mealPlanService = App.ServiceProvider?.GetRequiredService<MealPlanService>()!;

            if (SessionContext.CurrentUser != null)
            {
                UserNameTextBlock.Text = SessionContext.CurrentUser.Email;
            }

            MenuCalendar.SelectedDate = DateTime.Today;
        }

        public UserProfileWindow(MealPlanService mealPlanService)
        {
            InitializeComponent();
            _mealPlanService = mealPlanService;

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

            try
            {
                DateTime selectedDate = MenuCalendar.SelectedDate.Value;

                await _mealPlanService.AddCustomRecipeToPlanAsync(SessionContext.CurrentUser.ID, recipeName, selectedDate);

                MessageBox.Show($"Страву '{recipeName}' успішно збережено на {selectedDate:d MMMM}!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                if (recipeTextBox != null)
                {
                    recipeTextBox.Text = string.Empty;
                }

                var plans = await _mealPlanService.GetUserPlansForDateAsync(SessionContext.CurrentUser.ID, selectedDate);
                PlannedRecipesListBox.ItemsSource = plans;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка при додаванні плану страви");
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач вийшов із системи.");
            SessionContext.CurrentUser = null;
            this.Close();
        }
        private async void RecipeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is FourTaste.ApplicationCore.Entities.MealPlan mealPlan)
            {
                try
                {
                    // Оновлюємо статус в базі даних через сервіс
                    await _mealPlanService.UpdateMealPlanStatusAsync(mealPlan.Id, checkBox.IsChecked ?? false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Помилка при оновленні статусу страви");
                    MessageBox.Show($"Не вдалося оновити статус: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    }