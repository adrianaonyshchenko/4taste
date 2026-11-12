using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class CookingHistoryWindow : Window
    {
        public class CookingHistoryDisplayItem
        {
            public int? RecipeId { get; set; }
            public string RecipeName { get; set; } = string.Empty;
            public byte[]? Photo { get; set; }
            public string Category { get; set; } = string.Empty;
            public string Cuisine { get; set; } = string.Empty;
            public string Time { get; set; } = string.Empty;

            public MealPlan OriginalMealPlan { get; set; } = null!;
        }

        private readonly MealPlanService mealPlanService;
        private readonly IRecipeService recipeService;
        private readonly IRoomService? _roomService;
        private List<CookingHistoryDisplayItem> allCookedMeals = new ();

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public CookingHistoryWindow()
            : this(
                App.ServiceProvider?.GetService<MealPlanService>() ?? throw new InvalidOperationException("MealPlanService не зареєстровано."),
                App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не зареєстровано."))
        {
        }

        public CookingHistoryWindow(MealPlanService mealPlanService, IRecipeService recipeService)
            : this(mealPlanService, recipeService, App.ServiceProvider?.GetService<IRoomService>())
        {
        }

        public CookingHistoryWindow(MealPlanService mealPlanService, IRecipeService recipeService, IRoomService? roomService)
        {
            this.InitializeComponent();
            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            this._roomService = roomService;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateProfileAvatar();
            var roomsTask = this.LoadUserRoomsAsync();
            await this.LoadCookingHistoryAsync();
            await roomsTask;
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

        private async Task LoadCookingHistoryAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var result = await this.mealPlanService.GetCookingHistoryAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження історії", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rawMeals = result.Value ?? new List<MealPlan>();
            this.allCookedMeals.Clear();

            foreach (var meal in rawMeals)
            {
                byte[]? recipePhoto = null;
                int? targetRecipeId = meal.RecipeId;
                string category = string.Empty;
                string cuisine = string.Empty;
                string time = string.Empty;

                if (!targetRecipeId.HasValue || targetRecipeId.Value <= 0)
                {
                    if (!string.IsNullOrWhiteSpace(meal.RecipeName))
                    {
                        var searchResult = await this.recipeService.SearchRecipesAsync(RecipeSearchCriteria.Public(meal.RecipeName.Trim(), RecipeSearchFilter.TitleOnly));
                        if (searchResult.IsSuccess && searchResult.Value != null && searchResult.Value.Count > 0)
                        {
                            var foundRecipe = searchResult.Value.First();
                            targetRecipeId = foundRecipe.Id;
                            recipePhoto = foundRecipe.Photo;
                            category = foundRecipe.Category ?? string.Empty;
                            cuisine = foundRecipe.Cuisine ?? string.Empty;
                            time = foundRecipe.Time ?? string.Empty;
                        }
                    }
                }
                else
                {
                    var detailsResult = await this.recipeService.GetRecipeDetailsAsync(targetRecipeId.Value, this.CurrentUserId);
                    if (detailsResult.IsSuccess && detailsResult.Value?.Recipe != null)
                    {
                        var recipe = detailsResult.Value.Recipe;
                        recipePhoto = recipe.Photo;
                        category = recipe.Category ?? string.Empty;
                        cuisine = recipe.Cuisine ?? string.Empty;
                        time = recipe.Time ?? string.Empty;
                    }
                }

                this.allCookedMeals.Add(new CookingHistoryDisplayItem
                {
                    RecipeId = targetRecipeId,
                    RecipeName = meal.RecipeName ?? string.Empty,
                    Photo = recipePhoto,
                    Category = category,
                    Cuisine = cuisine,
                    Time = time,
                    OriginalMealPlan = meal ?? new MealPlan(),
                });
            }

            this.ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (this.CookingHistoryListBox == null || this.SearchTextBox == null)
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

                if (this.allCookedMeals.Count == 0)
                {
                    this.EmptyHistoryTextBlock.Text = "Ви ще не позначили жодної страви як приготовлену.\nЩоб додати страву до історії, відмітьте її у меню на календарі.";
                    this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                    this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.CookingHistoryListBox.ItemsSource = this.allCookedMeals;
                    this.CookingHistoryListBox.Visibility = Visibility.Visible;
                    this.EmptyHistoryTextBlock.Visibility = Visibility.Collapsed;
                }

                return;
            }

            var filtered = this.allCookedMeals.Where(m =>
                !string.IsNullOrEmpty(m.RecipeName) &&
                m.RecipeName.Contains(query, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (this.ResultsCountTextBlock != null)
            {
                this.ResultsCountTextBlock.Text = $"Знайдено: {filtered.Count}";
                this.ResultsCountTextBlock.Visibility = Visibility.Visible;
            }

            if (filtered.Count == 0)
            {
                this.EmptyHistoryTextBlock.Text = $"За запитом «{query}» страв не знайдено.\nСпробуйте змінити пошуковий запит.";
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.CookingHistoryListBox.ItemsSource = filtered;
                this.CookingHistoryListBox.Visibility = Visibility.Visible;
                this.EmptyHistoryTextBlock.Visibility = Visibility.Collapsed;
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

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }

        private void AddRecipeButton_Click(object sender, MouseButtonEventArgs e)
        {
            var createRecipeWindow = new CreateRecipeWindow(this.recipeService, this.CurrentUserId);
            if (createRecipeWindow.ShowDialog() == true)
            {
                var detailsWindow = new RecipeDetailsWindow(this.recipeService, createRecipeWindow.CreatedRecipeId);
                detailsWindow.ShowDialog();
            }

            _ = this.LoadCookingHistoryAsync();
        }

        private async void CreateRoomButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Увійдіть в акаунт, щоб створити кімнату.", "Авторизація", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var roomService = this._roomService ?? App.ServiceProvider?.GetService<IRoomService>();
            if (roomService is null)
            {
                MessageBox.Show("Сервіс кімнат недоступний.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var createRoomWindow = new CreateRoomWindow(roomService);
            createRoomWindow.Owner = this;

            if (createRoomWindow.ShowDialog() == true)
            {
                await this.LoadUserRoomsAsync();
            }
        }

        private async void RoomButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RoomDto roomDto)
            {
                var roomService = this._roomService ?? App.ServiceProvider?.GetService<IRoomService>();
                if (roomService is null)
                {
                    return;
                }

                var roomWindow = new RoomDetailsWindow(roomDto, roomService);
                roomWindow.Owner = this;
                if (roomWindow.ShowDialog() == true)
                {
                    await this.LoadUserRoomsAsync();
                }
            }
        }

        private async Task LoadUserRoomsAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                if (this.UserRoomsItemsControl != null)
                {
                    this.UserRoomsItemsControl.ItemsSource = null;
                }

                return;
            }

            var roomService = this._roomService ?? App.ServiceProvider?.GetService<IRoomService>();
            if (roomService is null)
            {
                return;
            }

            var result = await roomService.GetUserRoomsAsync(this.CurrentUserId);
            if (result.IsSuccess && this.UserRoomsItemsControl != null)
            {
                this.UserRoomsItemsControl.ItemsSource = result.Value;
            }
        }

        private void CookingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = App.ServiceProvider?.GetService<CookingHistoryWindow>() ?? new CookingHistoryWindow();
            historyWindow.Show();
            this.Close();
        }

        private void PublicRecipesButton_Click(object sender, RoutedEventArgs e)
        {
            var publicRecipesWindow = App.ServiceProvider?.GetService<PublicRecipesWindow>() ?? new PublicRecipesWindow(this.recipeService);
            publicRecipesWindow.Show();
            this.Close();
        }

        private void CookingHistoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.CookingHistoryListBox.SelectedItem is not CookingHistoryDisplayItem selectedItem)
            {
                return;
            }

            if (!selectedItem.RecipeId.HasValue || selectedItem.RecipeId.Value <= 0)
            {
                MessageBox.Show(
                    "Ця страва була додана вручну через календар, тому для неї немає збереженого рецепта в каталозі.",
                    "Рецепт недоступний",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.CookingHistoryListBox.SelectedItem = null;
                return;
            }

            var detailsWindow = new RecipeDetailsWindow(this.recipeService, selectedItem.RecipeId.Value);
            detailsWindow.ShowDialog();

            this.CookingHistoryListBox.SelectedItem = null;
        }
    }
}