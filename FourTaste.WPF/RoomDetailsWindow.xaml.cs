using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class RoomDetailsWindow : Window
    {
        private readonly RoomDto _room;
        private readonly IRoomService _roomService;
        private readonly IRoomMealPlanService _roomMealPlanService;
        private List<RoomMealPlan> _allMealPlans = new ();

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public RoomDetailsWindow(RoomDto room, IRoomService roomService)
            : this(room, roomService, App.ServiceProvider?.GetService<IRoomMealPlanService>() ?? throw new InvalidOperationException("IServiceProvider is not initialized."))
        {
        }

        public RoomDetailsWindow(RoomDto room, IRoomService roomService, IRoomMealPlanService roomMealPlanService)
        {
            InitializeComponent();
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _roomMealPlanService = roomMealPlanService ?? throw new ArgumentNullException(nameof(roomMealPlanService));

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

            string? inputRecipe = PromptInput("Скласти меню", "Введіть назву страви для спільного меню:");
            if (string.IsNullOrWhiteSpace(inputRecipe))
            {
                return;
            }

            var selectedDate = RoomCalendar.SelectedDate.Value;
            var addResult = await _roomMealPlanService.AddRoomMealPlanAsync(_room.Id, CurrentUserId, inputRecipe, null, selectedDate);
            if (addResult.IsFailure)
            {
                MessageBox.Show(addResult.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoadMealPlansAsync();
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

        private string? PromptInput(string title, string promptText)
        {
            var promptWindow = new Window
            {
                Title = title,
                Width = 350,
                Height = 160,
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
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new TextBox
            {
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(buttonPanel, 2);

            string? resultText = null;

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
                resultText = textBox.Text;
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
            cancelButton.Click += (s, ev) =>
            {
                promptWindow.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            promptWindow.Content = grid;
            textBox.Focus();

            if (promptWindow.ShowDialog() == true)
            {
                return resultText;
            }
            return null;
        }

        private void RoomCodeButton_Click(object sender, RoutedEventArgs e)
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

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
