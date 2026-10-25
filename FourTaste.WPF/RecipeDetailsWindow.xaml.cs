using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FourTaste.WPF
{
    public partial class RecipeDetailsWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly int _recipeId;

        public RecipeDetailsWindow(IRecipeService recipeService, int recipeId)
        {
            InitializeComponent();
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _recipeId = recipeId;
        }

        private static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int? viewerId = SessionContext.CurrentUser?.ID;

            var result = await _recipeService.GetRecipeDetailsAsync(_recipeId, viewerId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося відкрити рецепт", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }

            var details = result.Value!;
            var recipe = details.Recipe;

            TitleTextBlock.Text = recipe.Title;
            AuthorTextBlock.Text = details.AuthorName;
            TimeTextBlock.Text = $"Час приготування: {recipe.Time}";
            CategoryTextBlock.Text = $"Категорія: {recipe.Category}";
            CuisineTextBlock.Text = $"Країна: {recipe.Cuisine}";
            StatusTextBlock.Text = $"Статус: {(recipe.IsPublic ? "публічний" : "приватний")}";

            IngredientsItemsControl.ItemsSource = recipe.Ingredients;
            StepsItemsControl.ItemsSource = recipe.Steps;

            var photo = ToBitmapImage(recipe.Photo);
            if (photo != null)
            {
                RecipeImage.Source = photo;
                PhotoPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}