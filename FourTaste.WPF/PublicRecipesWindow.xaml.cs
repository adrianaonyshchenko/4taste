using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class PublicRecipesWindow : Window
    {
        private readonly IRecipeService recipeService;
        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public PublicRecipesWindow()
            : this(App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не зареєстровано."))
        {
        }

        public PublicRecipesWindow(IRecipeService recipeService)
        {
            InitializeComponent();
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateProfileAvatar();
            await this.LoadPublicRecipesAsync();
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

        private async Task LoadPublicRecipesAsync()
        {
            var result = await this.recipeService.GetPublicRecipesAsync();
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var recipes = result.Value ?? new List<Recipe>();
            if (recipes.Count == 0)
            {
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

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }

        private async void AddRecipeButton_Click(object sender, MouseButtonEventArgs e)
        {
            var createRecipeWindow = new CreateRecipeWindow(this.recipeService, this.CurrentUserId);
            if (createRecipeWindow.ShowDialog() == true)
            {
                await this.LoadPublicRecipesAsync();

                var detailsWindow = new RecipeDetailsWindow(this.recipeService, createRecipeWindow.CreatedRecipeId);
                detailsWindow.ShowDialog();
            }
        }

        private void CookingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = App.ServiceProvider?.GetService<CookingHistoryWindow>() ?? new CookingHistoryWindow();
            historyWindow.Show();
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
    }
}