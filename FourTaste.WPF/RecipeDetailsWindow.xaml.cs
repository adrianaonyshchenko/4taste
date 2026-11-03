using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class RecipeDetailsWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly IAllergyService? _allergyService;
        private readonly int _recipeId;

        public RecipeDetailsWindow(IRecipeService recipeService, int recipeId)
            : this(recipeService, App.ServiceProvider?.GetService<IAllergyService>(), recipeId)
        {
        }

        public RecipeDetailsWindow(IRecipeService recipeService, IAllergyService? allergyService, int recipeId)
        {
            InitializeComponent();
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _allergyService = allergyService;
            _recipeId = recipeId;
        }

        private static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatServingsText(int count)
        {
            int mod10 = count % 10;
            int mod100 = count % 100;

            if (mod10 == 1 && mod100 != 11)
            {
                return $"{count}\nпорція";
            }

            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
            {
                return $"{count}\nпорції";
            }

            return $"{count}\nпорцій";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRecipeDetailsAsync();
        }

        private async Task LoadRecipeDetailsAsync()
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

            int servings = recipe.Servings > 0 ? recipe.Servings : 1;
            ServingsTextBlock.Text = FormatServingsText(servings);

            bool isAuthor = viewerId.HasValue && viewerId.Value == recipe.UserId;
            EditRecipeButton.Visibility = isAuthor ? Visibility.Visible : Visibility.Collapsed;
            EditRecipeButton.IsEnabled = isAuthor;

            var userAllergies = new List<string>();
            if (viewerId.HasValue && _allergyService != null)
            {
                var allergiesRes = await _allergyService.GetAllergiesByUserIdAsync(viewerId.Value);
                if (allergiesRes.IsSuccess && allergiesRes.Value != null)
                {
                    userAllergies = allergiesRes.Value
                        .Where(a => !string.IsNullOrWhiteSpace(a.AllergenName))
                        .Select(a => a.AllergenName.Trim().ToLower())
                        .ToList();
                }
            }

            var ingredientViewModels = recipe.Ingredients.Select(ing =>
            {
                string name = ing.Name?.Trim() ?? string.Empty;
                string amount = string.IsNullOrWhiteSpace(ing.Amount) ? "1 шт." : ing.Amount.Trim();
                bool isAllergen = userAllergies.Any(al => !string.IsNullOrWhiteSpace(al) && name.ToLower().Contains(al));
                return new RecipeIngredientDisplayModel
                {
                    Name = name,
                    Amount = amount,
                    IsAllergen = isAllergen,
                };
            }).ToList();

            IngredientsItemsControl.ItemsSource = ingredientViewModels;
            StepsItemsControl.ItemsSource = recipe.Steps.OrderBy(s => s.StepNumber).ToList();

            var photo = ToBitmapImage(recipe.Photo);
            if (photo != null)
            {
                RecipeImage.Source = photo;
                PhotoPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecipeImage.Source = null;
                PhotoPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private async void EditRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditRecipeWindow(_recipeService, _allergyService, _recipeId)
            {
                Owner = this,
            };

            editWindow.ShowDialog();

            if (editWindow.IsDeleted)
            {
                this.Close();
            }
            else if (editWindow.IsSaved)
            {
                await LoadRecipeDetailsAsync();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
