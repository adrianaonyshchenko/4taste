using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FourTaste.WPF
{
    public partial class MyRecipesWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly int _currentUserId;
        private readonly ILogger<MyRecipesWindow>? _logger;

        public MyRecipesWindow(IRecipeService recipeService, int currentUserId, ILogger<MyRecipesWindow>? logger = null)
        {
            InitializeComponent();
            _recipeService = recipeService;
            _currentUserId = currentUserId;
            _logger = logger;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMyRecipesAsync();
        }

        private async Task LoadMyRecipesAsync()
        {
            try
            {
                var result = await _recipeService.GetRecipesByUserIdAsync(_currentUserId);
                if (result.IsSuccess)
                {
                    var recipes = result.Value ?? new List<Recipe>();
                    RecipesListBox.ItemsSource = recipes;

                    if (recipes.Count == 0)
                    {
                        EmptyTextBlock.Visibility = Visibility.Visible;
                        RecipesListBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        EmptyTextBlock.Visibility = Visibility.Collapsed;
                        RecipesListBox.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Помилка під час завантаження моїх рецептів.");
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var userProfileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            userProfileWindow.Show();
            this.Close();
        }
    }
}