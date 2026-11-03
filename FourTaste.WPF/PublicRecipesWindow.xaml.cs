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
    public partial class PublicRecipesWindow : Window
    {
        private const int SearchDelayMs = 300;

        private readonly IRecipeService recipeService;
        private int searchVersion;

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        private RecipeSearchFilter CurrentFilter
        {
            get
            {
                if (this.FilterTitleRadio?.IsChecked == true)
                {
                    return RecipeSearchFilter.TitleOnly;
                }

                if (this.FilterAuthorRadio?.IsChecked == true)
                {
                    return RecipeSearchFilter.AuthorOnly;
                }

                return RecipeSearchFilter.All;
            }
        }

        public PublicRecipesWindow()
            : this(App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не зареєстровано."))
        {
        }

        public PublicRecipesWindow(IRecipeService recipeService)
        {
            this.InitializeComponent();
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateProfileAvatar();
            this.UpdateSearchControls();

            if (this.CurrentUserId > 0 && (SessionContext.CurrentUser?.Avatar == null || SessionContext.CurrentUser.Avatar.Length == 0))
            {
                int uid = this.CurrentUserId;
                var dbBytes = await Task.Run(() => AvatarService.LoadAvatarBytes(uid));
                if (dbBytes != null && dbBytes.Length > 0 && SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == uid)
                {
                    SessionContext.CurrentUser.Avatar = dbBytes;
                    this.UpdateProfileAvatar();
                }
            }

            await this.SearchAsync(immediate: true);
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
            var filter = this.CurrentFilter;
            int version = ++this.searchVersion;

            if (!immediate)
            {
                await Task.Delay(SearchDelayMs);
                if (version != this.searchVersion)
                {
                    return;
                }
            }

            var result = await this.recipeService.SearchRecipesAsync(RecipeSearchCriteria.Public(query, filter));

            if (version != this.searchVersion)
            {
                return;
            }

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.ShowRecipes(result.Value ?? new List<Recipe>(), query, filter);
        }

        private void ShowRecipes(List<Recipe> recipes, string query, RecipeSearchFilter filter)
        {
            bool isSearching = !string.IsNullOrEmpty(query) || filter != RecipeSearchFilter.All;

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {recipes.Count}";
                this.ResultsCountTextBlock.Visibility = isSearching ? Visibility.Visible : Visibility.Collapsed;
            }

            if (recipes.Count == 0)
            {
                if (!isSearching)
                {
                    this.EmptyTextBlock.Text = "Поки немає жодного публічного рецепта.\nБудьте першим, хто поділиться рецептом!";
                }
                else if (string.IsNullOrEmpty(query))
                {
                    this.EmptyTextBlock.Text = "У цій категорії поки немає рецептів.";
                }
                else
                {
                    this.EmptyTextBlock.Text = $"За запитом «{query}» рецептів не знайдено.\nСпробуйте іншу назву страви або ім'я автора.";
                }

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

        private async void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }

            await this.SearchAsync(immediate: true);
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
                await this.SearchAsync(immediate: true);

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