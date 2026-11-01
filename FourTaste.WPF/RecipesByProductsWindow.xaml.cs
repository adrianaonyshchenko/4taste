using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.WPF
{
    public partial class RecipesByProductsWindow : Window
    {
        private const string AnyOption = "Усі";

        private readonly IRecipeService _recipeService;
        private readonly int _userId;

        public RecipesByProductsWindow(IRecipeService recipeService, int userId)
        {
            this.InitializeComponent();
            this._recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            this._userId = userId;

            this.CategoryComboBox.ItemsSource = new[] { AnyOption }.Concat(RecipeOptions.Categories).ToList();
            this.CategoryComboBox.SelectedIndex = 0;
            this.CuisineComboBox.ItemsSource = new[] { AnyOption }.Concat(RecipeOptions.Cuisines).ToList();
            this.CuisineComboBox.SelectedIndex = 0;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.SearchAsync();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await this.SearchAsync();
        }

        private async Task SearchAsync()
        {
            int? maxMinutes = null;
            string rawMinutes = this.MaxMinutesTextBox.Text.Trim();
            if (rawMinutes.Length > 0)
            {
                if (!int.TryParse(rawMinutes, out int parsed) || parsed <= 0)
                {
                    MessageBox.Show("Введіть максимальний час у хвилинах цілим додатним числом.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                maxMinutes = parsed;
            }

            var request = new FindRecipesByProductsRequest
            {
                UserId = this._userId,
                Category = this.SelectedOrNull(this.CategoryComboBox),
                Cuisine = this.SelectedOrNull(this.CuisineComboBox),
                MaxCookingMinutes = maxMinutes,
            };

            this.SearchButton.IsEnabled = false;
            try
            {
                var result = await this._recipeService.FindRecipesByProductsAsync(request);

                if (result.IsFailure)
                {
                    this.ShowMessage(result.Error ?? "Не вдалося виконати пошук.");
                    return;
                }

                var recipes = result.Value ?? new List<Recipe>();
                if (recipes.Count == 0)
                {
                    this.ShowMessage("За вашими продуктами рецептів не знайдено.\nСпробуйте змінити фільтри або додати більше продуктів.");
                    return;
                }

                this.RecipesListBox.ItemsSource = recipes;
                this.RecipesListBox.Visibility = Visibility.Visible;
                this.EmptyTextBlock.Visibility = Visibility.Collapsed;
            }
            finally
            {
                this.SearchButton.IsEnabled = true;
            }
        }

        private string? SelectedOrNull(System.Windows.Controls.ComboBox comboBox)
        {
            var value = comboBox.SelectedItem as string;
            return string.IsNullOrEmpty(value) || value == AnyOption ? null : value;
        }

        private void ShowMessage(string text)
        {
            this.RecipesListBox.ItemsSource = null;
            this.RecipesListBox.Visibility = Visibility.Collapsed;
            this.EmptyTextBlock.Text = text;
            this.EmptyTextBlock.Visibility = Visibility.Visible;
        }

        private void RecipesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.RecipesListBox.SelectedItem is Recipe selectedRecipe)
            {
                var detailsWindow = new RecipeDetailsWindow(this._recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();

                this.RecipesListBox.SelectedItem = null;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}