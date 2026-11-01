using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class MyRecipesWindow : Window
    {
        private const int SearchDelayMs = 300;

        private readonly IRecipeService recipeService;
        private readonly int currentUserId;
        private int searchVersion;

        public MyRecipesWindow()
            : this(
                App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не зареєстровано."),
                SessionContext.CurrentUser?.ID ?? 0)
        {
        }

        public MyRecipesWindow(IRecipeService recipeService, int currentUserId)
        {
            this.InitializeComponent();
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            this.currentUserId = currentUserId;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateSearchControls();
            await this.SearchAsync(immediate: true);
        }

        private void UpdateSearchControls()
        {
            if (this.SearchTextBox == null)
            {
                return;
            }

            bool hasText = !string.IsNullOrEmpty(this.SearchTextBox.Text);

            if (this.SearchPlaceholderTextBlock != null)
            {
                this.SearchPlaceholderTextBlock.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            }

            if (this.ClearSearchButton != null)
            {
                this.ClearSearchButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async Task SearchAsync(bool immediate)
        {
            string query = this.SearchTextBox.Text.Trim();
            int version = ++this.searchVersion;

            if (!immediate)
            {
                await Task.Delay(SearchDelayMs);
                if (version != this.searchVersion)
                {
                    return;
                }
            }

            var result = await this.recipeService.SearchRecipesAsync(RecipeSearchCriteria.ForUser(this.currentUserId, query));

            if (version != this.searchVersion)
            {
                return;
            }

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.ShowRecipes(result.Value ?? new List<Recipe>(), query);
        }

        private void ShowRecipes(List<Recipe> recipes, string query)
        {
            bool isSearching = !string.IsNullOrEmpty(query);

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {recipes.Count}";
                this.ResultsCountTextBlock.Visibility = isSearching ? Visibility.Visible : Visibility.Collapsed;
            }

            if (recipes.Count == 0)
            {
                this.EmptyTextBlock.Text = isSearching
                    ? $"За запитом «{query}» рецептів не знайдено.\nСпробуйте іншу назву страви."
                    : "У вас поки немає створених рецептів.\nДодайте свій перший рецепт!";

                this.EmptyTextBlock.Visibility = Visibility.Visible;
                this.RecipesListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.RecipesListBox.ItemsSource = recipes;
                this.RecipesListBox.Visibility = Visibility.Visible;
                this.EmptyTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private async void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.UpdateSearchControls();

            if (!this.IsLoaded)
            {
                return;
            }

            await this.SearchAsync(immediate: false);
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await this.SearchAsync(immediate: true);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                await this.ClearSearchAsync();
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await this.SearchAsync(immediate: true);
        }

        private async void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ClearSearchAsync();
        }

        private async Task ClearSearchAsync()
        {
            this.SearchTextBox.Text = string.Empty;
            this.SearchTextBox.Focus();
            await this.SearchAsync(immediate: true);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var userProfileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            userProfileWindow.Show();
            this.Close();
        }

        private void RecipesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.RecipesListBox.SelectedItem is Recipe selectedRecipe)
            {
                var detailsWindow = new RecipeDetailsWindow(this.recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();

                this.RecipesListBox.SelectedItem = null;
            }
        }

        private void RecipeCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is Recipe selectedRecipe)
            {
                var detailsWindow = new RecipeDetailsWindow(this.recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();
            }
        }
    }
}