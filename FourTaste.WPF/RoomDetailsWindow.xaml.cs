using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FourTaste.WPF
{
    public partial class RoomDetailsWindow : Window
    {
        private readonly RoomDto _room;
        private readonly IRoomService _roomService;
        private readonly IRoomMealPlanService _roomMealPlanService;
        private readonly IRecipeService _recipeService;
        private readonly ShoppingListService _shoppingListService;
        private List<RoomMealPlan> _allMealPlans = new ();

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public RoomDetailsWindow(RoomDto room, IRoomService roomService)
            : this(room, roomService, App.ServiceProvider?.GetService<IRoomMealPlanService>() ?? throw new InvalidOperationException("IServiceProvider is not initialized."), App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService is not initialized."), App.ServiceProvider?.GetService<ShoppingListService>() ?? throw new InvalidOperationException("ShoppingListService is not initialized."))
        {
        }

        public RoomDetailsWindow(RoomDto room, IRoomService roomService, IRoomMealPlanService roomMealPlanService, IRecipeService recipeService, ShoppingListService shoppingListService)
        {
            InitializeComponent();
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _roomMealPlanService = roomMealPlanService ?? throw new ArgumentNullException(nameof(roomMealPlanService));
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _shoppingListService = shoppingListService ?? throw new ArgumentNullException(nameof(shoppingListService));

            RoomCalendar.SelectedDate = DateTime.Today;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RoomNameTextBlock.Text = _room.Name;
            if (!string.IsNullOrWhiteSpace(_room.Description))
            {
                RoomDescriptionTextBlock.Text = _room.Description;
                RoomDescriptionTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                RoomDescriptionTextBlock.Visibility = Visibility.Collapsed;
            }

            bool isOwner = _room.OwnerId == this.CurrentUserId;
            DeleteRoomButton.Visibility = isOwner ? Visibility.Visible : Visibility.Collapsed;
            LeaveRoomButton.Visibility = isOwner ? Visibility.Collapsed : Visibility.Visible;

            if (RemoveMemberButton != null)
            {
                RemoveMemberButton.Visibility = isOwner ? Visibility.Visible : Visibility.Collapsed;
            }

            var result = await _roomService.GetRoomDetailsAsync(_room.Id, this.CurrentUserId);
            if (result.IsSuccess && result.Value != null)
            {
                MembersItemsControl.ItemsSource = result.Value.Members;
                if (!string.IsNullOrWhiteSpace(result.Value.Description))
                {
                    RoomDescriptionTextBlock.Text = result.Value.Description;
                    RoomDescriptionTextBlock.Visibility = Visibility.Visible;
                }
            }

            await LoadMealPlansAsync();
            await LoadShoppingListAsync();
        }

        private async Task LoadMealPlansAsync()
        {
            var result = await _roomMealPlanService.GetRoomMealPlansAsync(_room.Id);
            if (result.IsSuccess && result.Value != null)
            {
                _allMealPlans = result.Value;
                UpdateMealPlansDisplay();
            }
        }

        private async Task LoadShoppingListAsync()
        {
            var repo = App.ServiceProvider?.GetService<IShoppingListRepository>();
            if (repo != null)
            {
                var items = await repo.GetByRoomIdAsync(_room.Id);
                ShoppingListItemsControl.ItemsSource = items;
                ShoppingListPlaceholderText.Visibility = items == null || items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateMealPlansDisplay()
        {
            if (RoomCalendar.SelectedDate.HasValue)
            {
                var selectedDate = RoomCalendar.SelectedDate.Value.Date;
                var plansForDate = _allMealPlans
                    .Where(m => m.PlannedDate.Date == selectedDate)
                    .ToList();

                RoomMealPlansItemsControl.ItemsSource = plansForDate;
                SelectedDatePlaceholderText.Visibility = plansForDate.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                RoomMealPlansItemsControl.ItemsSource = null;
                SelectedDatePlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void RoomCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMealPlansDisplay();
        }

        private async void PlanMealButton_Click(object sender, RoutedEventArgs e)
        {
            if (!RoomCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show("Будь ласка, виберіть дату в календарі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var criteria = new RecipeSearchCriteria();
            var recipesResult = await _recipeService.SearchRecipesAsync(criteria);

            var availableRecipes = recipesResult.IsSuccess
                ? recipesResult.Value?.Where(r => r.UserId == CurrentUserId || r.IsPublic).ToList() ?? new List<Recipe>()
                : new List<Recipe>();

            var inputResult = PromptRecipeInput("Скласти меню", "Введіть назву або оберіть існуючий рецепт:", availableRecipes);

            if (inputResult == null || string.IsNullOrWhiteSpace(inputResult.Value.Name))
            {
                return;
            }

            var selectedDate = RoomCalendar.SelectedDate.Value;

            var addResult = await _roomMealPlanService.AddRoomMealPlanAsync(_room.Id, CurrentUserId, inputResult.Value.Name, inputResult.Value.RecipeId, selectedDate);

            if (addResult.IsFailure)
            {
                MessageBox.Show(this, addResult.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoadMealPlansAsync();
        }

        private async void GenerateShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await _shoppingListService.GenerateShoppingListFromMealPlanAsync(_room.Id);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Попередження, якщо у меню є страви, додані вручну
            if (!string.IsNullOrEmpty(result.Value))
            {
                MessageBox.Show(result.Value, "Увага (ручні страви)", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Список покупок успішно згенеровано зі спільного меню!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await LoadShoppingListAsync();
        }

        private async void ClearShoppingListButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Ви дійсно хочете очистити весь список покупок?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var repo = App.ServiceProvider?.GetService<IShoppingListRepository>();
            if (repo != null)
            {
                await repo.ClearRoomListAsync(_room.Id);
                await LoadShoppingListAsync();
            }
        }

        private async void DeleteMealPlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int mealPlanId)
            {
                var confirm = MessageBox.Show("Ви дійсно хочете видалити цю страву зі спільного меню?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                var result = await _roomMealPlanService.DeleteRoomMealPlanAsync(mealPlanId, CurrentUserId);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LoadMealPlansAsync();
            }
        }

        private (string Name, int? RecipeId)? PromptRecipeInput(string title, string promptText, List<Recipe> recipes)
        {
            var promptWindow = new Window
            {
                Title = title,
                Width = 380,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E5D3BD")!,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(14) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = promptText,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#4A352F")!,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var inputGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var comboBox = new ComboBox
            {
                IsEditable = true,
                IsTextSearchEnabled = true,
                DisplayMemberPath = "Title",
                SelectedValuePath = "Id",
                ItemsSource = recipes,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center


            };
            Grid.SetColumn(comboBox, 0);
            inputGrid.Children.Add(comboBox);

            var createBtn = new Button
            {
                Content = "+",
                Width = 28,
                Height = 28,
                Margin = new Thickness(8, 0, 0, 0),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#B89F8B")!,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                ToolTip = "Створити новий рецепт"
            };
            createBtn.Click += async (s, ev) =>
            {
                var createWin = new CreateRecipeWindow(_recipeService, CurrentUserId);
                if (createWin.ShowDialog() == true)
                {
                    var fresh = await _recipeService.SearchRecipesAsync(new RecipeSearchCriteria());
                    if (fresh.IsSuccess)
                    {
                        recipes = fresh.Value?.Where(r => r.UserId == CurrentUserId || r.IsPublic).ToList() ?? new List<Recipe>();
                        comboBox.ItemsSource = recipes;
                        comboBox.SelectedValue = createWin.CreatedRecipeId;
                    }
                }
            };
            Grid.SetColumn(createBtn, 1);
            inputGrid.Children.Add(createBtn);

            Grid.SetRow(inputGrid, 1);
            grid.Children.Add(inputGrid);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 2);

            (string Name, int? RecipeId)? resultData = null;

            var okButton = new Button
            {
                Content = "ОК",
                Width = 70,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#542E2B")!,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            okButton.Click += (s, ev) =>
            {
                string text = comboBox.Text?.Trim() ?? string.Empty;
                int? selectedId = comboBox.SelectedValue as int?;

                if (selectedId == null && !string.IsNullOrEmpty(text))
                {
                    var matched = recipes.FirstOrDefault(r => r.Title.Equals(text, StringComparison.OrdinalIgnoreCase));
                    if (matched != null)
                    {
                        selectedId = matched.Id;
                    }
                }

                resultData = (text, selectedId);
                promptWindow.DialogResult = true;
                promptWindow.Close();
            };

            var cancelButton = new Button
            {
                Content = "Скасувати",
                Width = 80,
                Height = 28,
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#877063")!,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };
            cancelButton.Click += (s, ev) => promptWindow.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            promptWindow.Content = grid;
            comboBox.Focus();

            if (promptWindow.ShowDialog() == true)
            {
                return resultData;
            }
            return null;
        }

        private async void RoomCodeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RoomCreatedDialog(
                _room.Name,
                _room.InviteCode,
                $"Код кімнати «{_room.Name}»",
                "Код кімнати");
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private async void DeleteRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Ви дійсно хочете видалити кімнату «{_room.Name}»?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _roomService.DeleteRoomAsync(_room.Id, this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Кімнату успішно видалено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private async void LeaveRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Ви дійсно хочете вийти з кімнати «{_room.Name}»?",
                "Підтвердження",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await _roomService.LeaveRoomAsync(_room.Id, this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Ви вийшли з кімнати.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void MealPlanBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && source is Button)
            {
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is RoomMealPlan mealPlan)
            {
                if (mealPlan.RecipeId.HasValue && mealPlan.RecipeId.Value > 0)
                {
                    var detailsWindow = new RecipeDetailsWindow(_recipeService, mealPlan.RecipeId.Value);
                    detailsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Цю страву було введено вручну, тому для неї немає детального опису рецепта.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
