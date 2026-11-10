using System;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class CreateRoomWindow : Window
    {
        private readonly IRoomService _roomService;

        public RoomDto? CreatedRoom { get; private set; }

        public CreateRoomWindow()
            : this(App.ServiceProvider?.GetRequiredService<IRoomService>() ?? throw new InvalidOperationException("IRoomService не зареєстровано."))
        {
        }

        public CreateRoomWindow(IRoomService roomService)
        {
            InitializeComponent();
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        }

        private void RoomNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NamePlaceholder != null)
            {
                NamePlaceholder.Visibility = string.IsNullOrEmpty(RoomNameTextBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void RoomDescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DescriptionPlaceholder != null)
            {
                DescriptionPlaceholder.Visibility = string.IsNullOrEmpty(RoomDescriptionTextBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            CreateButton.IsEnabled = false;

            try
            {
                string name = RoomNameTextBox.Text?.Trim() ?? string.Empty;
                string? description = string.IsNullOrWhiteSpace(RoomDescriptionTextBox.Text)
                    ? null
                    : RoomDescriptionTextBox.Text.Trim();

                var request = new CreateRoomRequest
                {
                    Name = name,
                    Description = description
                };

                var result = await _roomService.CreateRoomAsync(request);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CreatedRoom = result.Value;

                var dialog = new RoomCreatedDialog(result.Value!.Name, result.Value.InviteCode);
                dialog.Owner = this;
                dialog.ShowDialog();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Не вдалося з'єднатися з базою даних. Перевірте мережеве підключення або спробуйте пізніше.",
                    "Помилка зв'язку",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (CreateButton != null)
                {
                    CreateButton.IsEnabled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
