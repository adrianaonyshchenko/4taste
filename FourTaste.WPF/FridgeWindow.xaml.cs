using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FourTaste.WPF
{
    public partial class FridgeWindow : Window
    {
        private readonly IFridgeService _fridgeService;
        private readonly int _userId;
        private ObservableCollection<UserFridgeItem> _fridgeItems = new ();
        private ObservableCollection<UserShoppingItem> _shoppingItems = new ();

        public FridgeWindow(IFridgeService fridgeService, int userId)
        {
            this.InitializeComponent();
            this._fridgeService = fridgeService;
            this._userId = userId;

            this.FridgeItemsListBox.ItemsSource = _fridgeItems;
            this.ShoppingList.ItemsSource = _shoppingItems;

            _ = this.LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await this.LoadFridgeAsync();
            await this.LoadShoppingListAsync();
        }

        private async Task LoadFridgeAsync()
        {
            Log.Information("Завантаження продуктів холодильника для користувача {UserId}", _userId);
            var result = await _fridgeService.GetUserFridgeAsync(_userId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _fridgeItems.Clear();
            foreach (var item in result.Value!)
            {
                _fridgeItems.Add(item);
            }
        }

        private async Task LoadShoppingListAsync()
        {
            Log.Information("Завантаження списку покупок для користувача {UserId}", _userId);
            var result = await _fridgeService.GetShoppingListAsync(_userId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _shoppingItems.Clear();
            foreach (var item in result.Value!)
            {
                _shoppingItems.Add(item);
            }
        }

        private async void AddFridgeItemButton_Click(object sender, RoutedEventArgs e)
        {
            string ingredientName = this.NewIngredientTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ingredientName))
            {
                MessageBox.Show("Введіть назву продукту.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity = 1;
            if (!int.TryParse(this.NewQuantityTextBox.Text.Trim(), out quantity) || quantity <= 0)
            {
                quantity = 1;
            }

            string unit = this.NewUnitTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "шт";
            }

            var request = new AddFridgeItemRequest
            {
                UserId = _userId,
                IngredientName = ingredientName,
                Quantity = quantity,
                Unit = unit
            };

            var result = await _fridgeService.AddItemAsync(request);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.NewIngredientTextBox.Clear();
            this.NewQuantityTextBox.Text = "1";
            this.NewUnitTextBox.Text = "шт";
            await this.LoadFridgeAsync();
        }

        private async void DeleteFridgeItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.FridgeItemsListBox.SelectedItem is not UserFridgeItem selectedItem)
            {
                MessageBox.Show("Оберіть продукт для видалення зі списку 'У мене є'.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var request = new RemoveFridgeItemRequest
            {
                UserId = _userId,
                ItemId = selectedItem.Id
            };

            var result = await _fridgeService.RemoveItemAsync(request);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadFridgeAsync();
        }

        private async void AddShoppingItemButton_Click(object sender, RoutedEventArgs e)
        {
            string itemName = this.NewShoppingItemTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("Введіть назву продукту для купівлі.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity = 1;
            if (!int.TryParse(this.NewShoppingQuantityTextBox.Text.Trim(), out quantity) || quantity <= 0)
            {
                quantity = 1;
            }

            string unit = this.NewShoppingUnitTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "шт";
            }

            var request = new AddShoppingItemRequest
            {
                UserId = _userId,
                IngredientName = itemName,
                Quantity = quantity,
                Unit = unit
            };

            var result = await _fridgeService.AddShoppingItemAsync(request);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.NewShoppingItemTextBox.Clear();
            this.NewShoppingQuantityTextBox.Text = "1";
            this.NewShoppingUnitTextBox.Text = "шт";
            await this.LoadShoppingListAsync();
        }

        private async void DeleteShoppingItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ShoppingList.SelectedItem is not UserShoppingItem selectedItem)
            {
                MessageBox.Show("Оберіть продукт зі списку 'Купити' для видалення.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var request = new RemoveShoppingItemRequest
            {
                UserId = _userId,
                ItemId = selectedItem.Id
            };

            var result = await _fridgeService.RemoveShoppingItemAsync(request);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadShoppingListAsync();
        }

        private void FindRecipeForMeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Користувач натиснув 'Знайти рецепт для мене' на основі холодильника");

            var recipeService = App.ServiceProvider!.GetRequiredService<IRecipeService>();
            var window = new RecipesByProductsWindow(recipeService, _userId) { Owner = this };
            window.ShowDialog();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = Application.Current.Windows.OfType<UserProfileWindow>().FirstOrDefault();

            if (profileWindow == null)
            {
                profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            }

            if (profileWindow.WindowState == WindowState.Minimized)
            {
                profileWindow.WindowState = WindowState.Normal;
            }

            profileWindow.Show();
            profileWindow.Activate();
            this.Close();
        }
    }
}