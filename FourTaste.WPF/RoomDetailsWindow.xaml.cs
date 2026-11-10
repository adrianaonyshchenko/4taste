using System;
using System.Windows;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;

namespace FourTaste.WPF
{
    public partial class RoomDetailsWindow : Window
    {
        private readonly RoomDto _room;
        private readonly IRoomService _roomService;

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public RoomDetailsWindow(RoomDto room, IRoomService roomService)
        {
            InitializeComponent();
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
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
