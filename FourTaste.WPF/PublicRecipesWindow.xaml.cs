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
    public partial class PublicRecipesWindow : Window
    {
        private readonly IRecipeService recipeService;
        private List<Recipe> allPublicRecipes = new ();

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

            this.allPublicRecipes = result.Value ?? new List<Recipe>();
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
            var filter = this.CurrentFilter;

            if (string.IsNullOrEmpty(query) && filter == RecipeSearchFilter.All)
            {
                if (this.ResultsCountTextBlock != null)
                {
                    this.ResultsCountTextBlock.Visibility = Visibility.Collapsed;
                }

                if (this.allPublicRecipes.Count == 0)
                {
                    this.EmptyTextBlock.Text = "Поки немає жодного публічного рецепта.\nБудьте першим, хто поділиться рецептом!";
                    this.EmptyTextBlock.Visibility = Visibility.Visible;
                    this.RecipesListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.RecipesListBox.ItemsSource = this.allPublicRecipes;
                    this.RecipesListBox.Visibility = Visibility.Visible;
                    this.EmptyTextBlock.Visibility = Visibility.Collapsed;
                }

                return;
            }

            string queryWithoutAt = query.StartsWith('@') ? query.Substring(1) : query;

            var filtered = this.allPublicRecipes.Where(r =>
            {
                if (string.IsNullOrEmpty(query))
                {
                    return true;
                }

                bool matchTitle = filter != RecipeSearchFilter.AuthorOnly &&
                    !string.IsNullOrEmpty(r.Title) &&
                    r.Title.Contains(query, StringComparison.OrdinalIgnoreCase);

                bool matchAuthorName = filter != RecipeSearchFilter.TitleOnly &&
                    !string.IsNullOrEmpty(r.AuthorName) &&
                    r.AuthorName.Contains(query, StringComparison.OrdinalIgnoreCase);

                bool matchAuthorUsername = filter != RecipeSearchFilter.TitleOnly &&
                    !string.IsNullOrEmpty(r.AuthorUsername) &&
                    (r.AuthorUsername.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                     r.AuthorUsername.Contains(queryWithoutAt, StringComparison.OrdinalIgnoreCase));

                return matchTitle || matchAuthorName || matchAuthorUsername;
            }).ToList();

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {filtered.Count}";
                this.ResultsCountTextBlock.Visibility = Visibility.Visible;
            }

            if (filtered.Count == 0)
            {
                this.EmptyTextBlock.Text = string.IsNullOrEmpty(query)
                    ? "У цій категорії поки немає рецептів."
                    : $"За запитом «{query}» рецептів не знайдено.\nСпробуйте іншу назву страви або ім'я автора.";
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

        private void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            this.ApplyFilter();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }

        private void AddRecipeButton_Click(object sender, MouseButtonEventArgs e)
        {
            var createRecipeWindow = new CreateRecipeWindow(this.recipeService, this.CurrentUserId);
            createRecipeWindow.ShowDialog();

            _ = this.LoadPublicRecipesAsync();
        }

        private void CookingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = App.ServiceProvider?.GetService<CookingHistoryWindow>() ?? new CookingHistoryWindow();
            historyWindow.Show();
            this.Close();
        }
    }
}
