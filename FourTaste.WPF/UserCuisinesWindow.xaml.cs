using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FourTaste.WPF
{
    public partial class UserCuisinesWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly int _userId;

        public UserCuisinesWindow(IRecipeService recipeService, int userId)
        {
            this.InitializeComponent();
            this._recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            this._userId = userId;
            this.Loaded += this.UserCuisinesWindow_Loaded;
        }

        private async void UserCuisinesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await this._recipeService.SearchRecipesAsync(RecipeSearchCriteria.ForUser(this._userId, string.Empty));

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var recipes = result.Value ?? new List<Recipe>();

            var stats = recipes
                .GroupBy(r => string.IsNullOrWhiteSpace(r.Cuisine) ? "Інша" : r.Cuisine)
                .Select(g => new CuisineStatModel
                {
                    CuisineName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            this.CuisinesListBox.ItemsSource = stats;
        }
    }
}