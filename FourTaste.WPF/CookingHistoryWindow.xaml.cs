using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class CookingHistoryWindow : Window
    {
        private readonly MealPlanService mealPlanService;
        private readonly IRecipeService recipeService;
        private List<MealPlan> allCookedMeals = new ();

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public CookingHistoryWindow()
            : this(
                App.ServiceProvider?.GetService<MealPlanService>() ?? throw new InvalidOperationException("MealPlanService не зареєстровано."),
                App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не зареєстровано."))
        {
        }

        public CookingHistoryWindow(MealPlanService mealPlanService, IRecipeService recipeService)
        {
            this.InitializeComponent();
            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateProfileAvatar();
            await this.LoadCookingHistoryAsync();
        }

        private void UpdateProfileAvatar()
        {
            if (this.CurrentUserId <= 0)
            {
                return;
            }

            var avatarImage = AvatarService.LoadAvatar(this.CurrentUserId);
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

        private async Task LoadCookingHistoryAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var result = await this.mealPlanService.GetCookingHistoryAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження історії", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.allCookedMeals = result.Value ?? new List<MealPlan>();
            this.ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (this.CookingHistoryListBox == null || this.SearchTextBox == null)
            {
                return;
            }

            string rawText = this.SearchTextBox.Text;
            bool hasText = !string.IsNullOrEmpty(rawText);

            if (this.SearchPlaceholderTextBlock != null)
            {
                this.SearchPlaceholderTextBlock.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            }

            if (this.ClearSearchButton != null)
            {
                this.ClearSearchButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
            }

            string query = rawText.Trim();

            if (string.IsNullOrEmpty(query))
            {
                if (this.ResultsCountTextBlock != null)
                {
                    this.ResultsCountTextBlock.Visibility = Visibility.Collapsed;
                }

                if (this.allCookedMeals.Count == 0)
                {
                    this.EmptyHistoryTextBlock.Text = "Ви ще не позначили жодної страви як приготовлену.\nЩоб додати страву до історії, відмітьте її у меню на календарі.";
                    this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                    this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.CookingHistoryListBox.ItemsSource = this.allCookedMeals;
                    this.CookingHistoryListBox.Visibility = Visibility.Visible;
                    this.EmptyHistoryTextBlock.Visibility = Visibility.Collapsed;
                }

                return;
            }

            var filtered = this.allCookedMeals.Where(m =>
                !string.IsNullOrEmpty(m.RecipeName) &&
                m.RecipeName.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {filtered.Count}";
                this.ResultsCountTextBlock.Visibility = Visibility.Visible;
            }

            if (filtered.Count == 0)
            {
                this.EmptyHistoryTextBlock.Text = $"За запитом «{query}» страв не знайдено.\nСпробуйте змінити пошуковий запит.";
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.CookingHistoryListBox.ItemsSource = filtered;
                this.CookingHistoryListBox.Visibility = Visibility.Visible;
                this.EmptyHistoryTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.ApplyFilter();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.ApplyFilter();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                this.ClearSearch();
                e.Handled = true;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            this.ApplyFilter();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClearSearch();
        }

        private void ClearSearch()
        {
            this.SearchTextBox.Text = string.Empty;
            this.SearchTextBox.Focus();
            this.ApplyFilter();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }

        private void AddRecipeButton_Click(object sender, MouseButtonEventArgs e)
        {
            var createRecipeWindow = new CreateRecipeWindow(this.recipeService, this.CurrentUserId);
            createRecipeWindow.ShowDialog();

            _ = this.LoadCookingHistoryAsync();
        }

        private void CookingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = App.ServiceProvider?.GetService<CookingHistoryWindow>() ?? new CookingHistoryWindow();
            historyWindow.Show();
            this.Close();
        }

        private void PublicRecipesButton_Click(object sender, RoutedEventArgs e)
        {
            var publicRecipesWindow = App.ServiceProvider?.GetService<PublicRecipesWindow>() ?? new PublicRecipesWindow(this.recipeService);
            publicRecipesWindow.Show();
            this.Close();
        }
    }
}
