using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Services;

namespace FourTaste.WPF.ViewModels
{
    public sealed class ShoppingListViewModel : INotifyPropertyChanged
    {
        private readonly ShoppingListService _shoppingListService;
        private readonly int _roomId;

        private ShoppingListItem? _selectedItem;
        private bool _isBusy;
        private string? _errorMessage;

        public ShoppingListViewModel(int roomId, ShoppingListService shoppingListService)
        {
            if (roomId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roomId));
            }

            _roomId = roomId;
            _shoppingListService = shoppingListService ?? throw new ArgumentNullException(nameof(shoppingListService));

            Items = new ObservableCollection<ShoppingListItem>();

            AddItemCommand = new RelayCommand(
                execute: () => _ = AddManualItemAsync(string.Empty, 1m, "шт"),
                canExecute: () => !_isBusy);

            RemoveSelectedItemCommand = new RelayCommand(
                execute: () => _ = RemoveSelectedItemAsync(),
                canExecute: () => !_isBusy && SelectedItem != null);

            GenerateFromMealPlanCommand = new RelayCommand(
                execute: () => _ = GenerateFromMealPlanAsync(),
                canExecute: () => !_isBusy);

            ClearListCommand = new RelayCommand(
                execute: () => _ = ClearListAsync(),
                canExecute: () => !_isBusy && Items.Count > 0);

            Items.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(IsEmpty));
                ClearListCommand.RaiseCanExecuteChanged();
                RemoveSelectedItemCommand.RaiseCanExecuteChanged();
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action<string, bool>? NotificationRequested;

        public ObservableCollection<ShoppingListItem> Items { get; }

        public bool IsEmpty => Items.Count == 0;

        public ShoppingListItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Equals(_selectedItem, value))
                {
                    return;
                }

                _selectedItem = value;
                OnPropertyChanged();
                RemoveSelectedItemCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
                AddItemCommand.RaiseCanExecuteChanged();
                RemoveSelectedItemCommand.RaiseCanExecuteChanged();
                GenerateFromMealPlanCommand.RaiseCanExecuteChanged();
                ClearListCommand.RaiseCanExecuteChanged();
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage == value)
                {
                    return;
                }

                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddItemCommand { get; }

        public RelayCommand RemoveSelectedItemCommand { get; }

        public RelayCommand GenerateFromMealPlanCommand { get; }

        public RelayCommand ClearListCommand { get; }

        public async Task LoadAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _shoppingListService.GetByRoomIdAsync(_roomId);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                IsBusy = false;
                return;
            }

            SelectedItem = null;
            Items.Clear();
            if (result.Value != null)
            {
                foreach (var item in result.Value)
                {
                    Items.Add(item);
                }
            }

            OnPropertyChanged(nameof(IsEmpty));
            ClearListCommand.RaiseCanExecuteChanged();
            RemoveSelectedItemCommand.RaiseCanExecuteChanged();
            IsBusy = false;
        }

        public async Task AddManualItemAsync(string productName, decimal quantity, string unit)
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _shoppingListService.AddManualItemAsync(_roomId, productName, quantity, unit);
            IsBusy = false;

            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                NotificationRequested?.Invoke(result.Error ?? "Не вдалося додати продукт.", true);
                return;
            }

            if (result.Value != null)
            {
                Items.Add(result.Value);
                SelectedItem = result.Value;
                OnPropertyChanged(nameof(IsEmpty));
                ClearListCommand.RaiseCanExecuteChanged();
            }
        }

        public async Task AddManualItemFromInputAsync(string productName, string quantityText, string unit)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                NotificationRequested?.Invoke("Введіть назву продукту.", true);
                return;
            }

            decimal quantity = 1m;
            if (!string.IsNullOrWhiteSpace(quantityText))
            {
                string normalized = quantityText.Trim().Replace(',', '.');
                if (!decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) || parsed <= 0)
                {
                    NotificationRequested?.Invoke("Кількість має бути більшою за 0.", true);
                    return;
                }

                quantity = parsed;
            }

            await AddManualItemAsync(productName, quantity, unit);
        }

        public async Task GenerateFromMealPlanAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _shoppingListService.GenerateShoppingListFromMealPlanAsync(_roomId);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                IsBusy = false;
                NotificationRequested?.Invoke(result.Error ?? "Помилка генерації списку.", true);
                return;
            }

            await LoadAsync();

            if (!string.IsNullOrEmpty(result.Value))
            {
                NotificationRequested?.Invoke(result.Value!, false);
            }
            else
            {
                NotificationRequested?.Invoke("Список покупок успішно згенеровано зі спільного меню!", false);
            }
        }

        public async Task ClearListAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await _shoppingListService.ClearListAsync(_roomId);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                IsBusy = false;
                NotificationRequested?.Invoke(result.Error ?? "Не вдалося очистити список покупок.", true);
                return;
            }

            SelectedItem = null;
            Items.Clear();
            OnPropertyChanged(nameof(IsEmpty));
            ClearListCommand.RaiseCanExecuteChanged();
            RemoveSelectedItemCommand.RaiseCanExecuteChanged();
            IsBusy = false;
        }

        public async Task RemoveSelectedItemAsync()
        {
            if (SelectedItem == null)
            {
                return;
            }

            var itemToRemove = SelectedItem;
            IsBusy = true;
            ErrorMessage = null;

            var result = await _shoppingListService.RemoveItemAsync(_roomId, itemToRemove.Id);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                IsBusy = false;
                NotificationRequested?.Invoke(result.Error ?? "Не вдалося видалити продукт.", true);
                return;
            }

            if (Equals(SelectedItem, itemToRemove))
            {
                SelectedItem = null;
            }

            Items.Remove(itemToRemove);
            OnPropertyChanged(nameof(IsEmpty));
            ClearListCommand.RaiseCanExecuteChanged();
            RemoveSelectedItemCommand.RaiseCanExecuteChanged();
            IsBusy = false;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}