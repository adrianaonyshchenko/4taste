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
    public partial class MyRecipesWindow : Window
    {
        private readonly IRecipeService recipeService;
        private readonly int currentUserId;
        private List<Recipe> allMyRecipes = new ();

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
            await this.LoadMyRecipesAsync();
        }

        private async Task LoadMyRecipesAsync()
        {
            var result = await this.recipeService.GetRecipesByUserIdAsync(this.currentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.allMyRecipes = result.Value ?? new List<Recipe>();
            this.ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (this.RecipesListBox == null || this.SearchTextBox == null)
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

                if (this.allMyRecipes.Count == 0)
                {
                    this.EmptyTextBlock.Text = "У вас поки немає створених рецептів.\nДодайте свій перший рецепт!";
                    this.EmptyTextBlock.Visibility = Visibility.Visible;
                    this.RecipesListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.RecipesListBox.ItemsSource = this.allMyRecipes;
                    this.RecipesListBox.Visibility = Visibility.Visible;
                    this.EmptyTextBlock.Visibility = Visibility.Collapsed;
                }

                return;
            }

            var filtered = this.allMyRecipes.Where(r =>
                (!string.IsNullOrEmpty(r.Title) && r.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.Category) && r.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(r.Cuisine) && r.Cuisine.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {filtered.Count}";
                this.ResultsCountTextBlock.Visibility = Visibility.Visible;
            }

            if (filtered.Count == 0)
            {
                this.EmptyTextBlock.Text = $"За запитом «{query}» рецептів не знайдено.\nСпробуйте іншу назву страви.";
                this.EmptyTextBlock.Visibility = Visibility.Visible;
                this.RecipesListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.RecipesListBox.ItemsSource = filtered;
                this.RecipesListBox.Visibility = Visibility.Visible;
                this.EmptyTextBlock.Visibility = Visibility.Collapsed;
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
                var detailsWindow = new RecipeDetailsWindow(_recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();

                this.RecipesListBox.SelectedItem = null;
            }
        }

        private void RecipeCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is Recipe selectedRecipe)
            {
                var detailsWindow = new RecipeDetailsWindow(_recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();
            }
        }
    }
}
