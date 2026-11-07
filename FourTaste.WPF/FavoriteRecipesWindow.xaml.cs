using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Serilog;

namespace FourTaste.WPF
{
    public partial class FavoriteRecipesWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly int _userId;

        public FavoriteRecipesWindow(IRecipeService recipeService, int userId)
        {
            InitializeComponent();
            _recipeService = recipeService;
            _userId = userId;

            LoadFavoriteRecipesAsync();
        }

        private async void LoadFavoriteRecipesAsync()
        {
            Log.Information("Завантаження обраних рецептів для користувача {UserId}", _userId);
            var result = await _recipeService.GetFavoriteRecipesAsync(_userId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FavoriteRecipesListBox.ItemsSource = result.Value;
        }

        private void FavoriteRecipesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FavoriteRecipesListBox.SelectedItem is Recipe selectedRecipe)
            {
                Log.Information("Користувач обрав рецепт {RecipeId} зі списку обраних, відкриваємо деталі.", selectedRecipe.Id);

                var detailsWindow = new RecipeDetailsWindow(_recipeService, selectedRecipe.Id);
                detailsWindow.ShowDialog();

                LoadFavoriteRecipesAsync();

                FavoriteRecipesListBox.SelectedItem = null;
            }
        }

        private void BackToProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }
    }
}