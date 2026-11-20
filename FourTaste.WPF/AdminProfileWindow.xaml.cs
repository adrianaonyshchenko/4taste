using System;
using System.Windows;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class AdminProfileWindow : Window
    {
        private readonly IAuthService authService;
        private readonly UserService userService;

        public AdminProfileWindow()
            : this(
                App.ServiceProvider?.GetService<IAuthService>() ?? throw new InvalidOperationException("IAuthService не зареєстровано."),
                App.ServiceProvider?.GetService<UserService>() ?? throw new InvalidOperationException("UserService не зареєстровано."))
        {
        }

        public AdminProfileWindow(IAuthService authService, UserService userService)
        {
            this.InitializeComponent();
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var admin = App.CurrentUser.CurrentUser;
            if (!App.CurrentUser.IsAdmin || admin is null)
            {
                MessageBox.Show("Ця сторінка доступна лише адміністраторам.", "Недостатньо прав", MessageBoxButton.OK, MessageBoxImage.Warning);
                App.CreateHomeWindow().Show();
                this.Close();
                return;
            }

            this.UserNameTextBlock.Text = string.IsNullOrWhiteSpace(admin.Name) ? admin.Email : admin.Name;
            this.UserNicknameTextBlock.Text = $"@{admin.Username.TrimStart('@')}";
            this.UserEmailTextBlock.Text = admin.Email;

            var avatar = App.Avatars.LoadAvatar(admin.ID);
            this.UserAvatarImageBrush.ImageSource = avatar;
            this.CustomAvatarBorder.Visibility = avatar is null ? Visibility.Collapsed : Visibility.Visible;
            this.DefaultAvatarBorder.Visibility = avatar is null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            App.CreateHomeWindow().Show();
            this.Close();
        }

        private void EditDataButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = App.ServiceProvider?.GetService<EditProfileWindow>() ?? new EditProfileWindow();
            editWindow.Show();
            this.Close();
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Ви дійсно бажаєте видалити свій профіль адміністратора? Цю дію неможливо буде скасувати.",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.userService.DeleteAccountAsync(App.CurrentUser.UserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка видалення профілю", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Профіль успішно видалено.", "Видалення", MessageBoxButton.OK, MessageBoxImage.Information);
            await this.LogoutAndShowLoginAsync();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.LogoutButton.IsEnabled = false;
            try
            {
                await this.LogoutAndShowLoginAsync();
            }
            finally
            {
                this.LogoutButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task LogoutAndShowLoginAsync()
        {
            await this.authService.LogoutAsync();
            var loginWindow = App.ServiceProvider?.GetService<LoginWindow>() ?? new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
