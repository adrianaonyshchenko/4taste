using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace FourTaste.WPF
{
    public partial class EditProfileWindow : Window
    {
        private readonly UserService userService;

        private int CurrentUserId => App.CurrentUser.UserId;

        public EditProfileWindow()
            : this(App.ServiceProvider?.GetService<UserService>() ?? throw new InvalidOperationException("UserService не зареєстровано."))
        {
        }

        public EditProfileWindow(UserService userService)
        {
            this.InitializeComponent();
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateAvatarPreview();
            await this.LoadUserProfileAsync();
        }

        private void UpdateAvatarPreview()
        {
            var avatarImage = App.Avatars.LoadAvatar(this.CurrentUserId);
            if (avatarImage != null)
            {
                this.AvatarImageBrush.ImageSource = avatarImage;
                this.CustomAvatarBorder.Visibility = Visibility.Visible;
                this.DefaultAvatarBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.AvatarImageBrush.ImageSource = null;
                this.CustomAvatarBorder.Visibility = Visibility.Collapsed;
                this.DefaultAvatarBorder.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadUserProfileAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                return;
            }

            var result = await this.userService.GetUserByIdAsync(this.CurrentUserId);
            if (result.IsSuccess && result.Value is { } user)
            {
                this.ProfileNameTextBox.Text = user.Name;
                this.ProfileUsernameTextBox.Text = user.Username;
                this.ProfileEmailTextBox.Text = user.Email;
            }
            else if (App.CurrentUser.CurrentUser != null)
            {
                this.ProfileNameTextBox.Text = App.CurrentUser.CurrentUser.Name;
                this.ProfileUsernameTextBox.Text = App.CurrentUser.CurrentUser.Username;
                this.ProfileEmailTextBox.Text = App.CurrentUser.CurrentUser.Email;
            }
        }

        private async void ChooseAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Оберіть зображення для аватарки",
                Filter = "Зображення (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Усі файли (*.*)|*.*",
            };

            if (dialog.ShowDialog() == true)
            {
                bool saved = App.Avatars.SaveAvatar(this.CurrentUserId, dialog.FileName);
                if (saved)
                {
                    if (App.CurrentUser.CurrentUser?.Avatar != null)
                    {
                        await this.userService.UpdateAvatarAsync(this.CurrentUserId, App.CurrentUser.CurrentUser.Avatar);
                    }

                    this.UpdateAvatarPreview();
                    MessageBox.Show("Аватарку успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не вдалося зберегти обране зображення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!App.Avatars.HasAvatar(this.CurrentUserId))
            {
                MessageBox.Show("Аватарка ще не встановлена.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show("Ви дійсно бажаєте видалити аватарку?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            App.Avatars.DeleteAvatar(this.CurrentUserId);
            await this.userService.UpdateAvatarAsync(this.CurrentUserId, null);
            this.UpdateAvatarPreview();
            MessageBox.Show("Аватарку успішно видалено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void UpdateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string name = this.ProfileNameTextBox.Text;
            string username = this.ProfileUsernameTextBox.Text;
            string email = this.ProfileEmailTextBox.Text;

            var result = await this.userService.UpdateProfileAsync(this.CurrentUserId, name, username, email);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка оновлення профілю", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (App.CurrentUser.CurrentUser != null)
            {
                App.CurrentUser.CurrentUser.Name = name.Trim();
                App.CurrentUser.CurrentUser.Username = username.Trim();
                App.CurrentUser.CurrentUser.Email = email.Trim();
            }

            MessageBox.Show("Особисті дані успішно оновлено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
            this.NavigateToProfile();
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CurrentUserId <= 0)
            {
                MessageBox.Show("Користувач не авторизований.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string newPassword = this.NewPasswordBox.Password;
            var result = await this.userService.ChangePasswordAsync(this.CurrentUserId, newPassword);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка зміни пароля", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.NewPasswordBox.Clear();
            MessageBox.Show("Пароль успішно змінено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProfileField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.UpdateProfileButton_Click(this.UpdateProfileButton, new RoutedEventArgs());
            }
        }

        private void NewPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                this.ChangePasswordButton_Click(this.ChangePasswordButton, new RoutedEventArgs());
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigateToProfile();
        }

        private void NavigateToProfile()
        {
            App.CreateProfileWindow().Show();
            this.Close();
        }
    }
}
