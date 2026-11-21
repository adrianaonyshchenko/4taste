using System;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class AdminUserDetailsWindow : Window
    {
        private readonly IUserModerationService moderationService;
        private readonly int targetUserId;
        private User? target;

        public AdminUserDetailsWindow(int targetUserId)
            : this(
                App.ServiceProvider?.GetService<IUserModerationService>() ?? throw new InvalidOperationException("IUserModerationService не зареєстровано."),
                targetUserId)
        {
        }

        public AdminUserDetailsWindow(IUserModerationService moderationService, int targetUserId)
        {
            this.InitializeComponent();
            this.moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
            this.targetUserId = targetUserId;
        }

        private bool CanModerate =>
            this.target is { } user && user.Role != UserRole.Admin && user.ID != App.CurrentUser.UserId;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.CurrentUser.IsAdmin)
            {
                MessageBox.Show("Ця сторінка доступна лише адміністраторам.", "Недостатньо прав", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.GoHome();
                return;
            }

            await this.LoadAsync();
        }

        private async Task LoadAsync()
        {
            var result = await this.moderationService.GetUserAsync(this.targetUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося відкрити профіль", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.GoHome();
                return;
            }

            this.target = result.Value!;
            this.Render();
        }

        private void Render()
        {
            var user = this.target!;
            string displayName = string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name;

            this.NameTextBlock.Text = displayName;
            this.NicknameTextBlock.Text = $"@{user.Username.TrimStart('@')}";
            this.EmailTextBlock.Text = user.Email;
            this.AdminBadge.Visibility = user.Role == UserRole.Admin ? Visibility.Visible : Visibility.Collapsed;
            this.InitialTextBlock.Text = displayName.Trim().Length > 0 ? displayName.Trim()[0].ToString().ToUpperInvariant() : "?";

            this.RegisteredTextBlock.Text = user.CreatedAt is { } created
                ? $"Дата реєстрації: {created.ToLocalTime():dd.MM.yyyy}"
                : "Дата реєстрації: —";
            this.StatusTextBlock.Text = user.IsBlocked ? "Статус: заблокований" : "Статус: активний";
            this.ReasonTextBlock.Text = $"Причина: {user.BlockedReason}";
            this.ReasonTextBlock.Visibility = user.IsBlocked && !string.IsNullOrWhiteSpace(user.BlockedReason)
                ? Visibility.Visible
                : Visibility.Collapsed;

            this.BlockButton.Content = user.IsBlocked ? "Розблокувати" : "Заблокувати";
            this.HintTextBlock.Visibility = this.CanModerate ? Visibility.Collapsed : Visibility.Visible;
            this.SetBusy(false);

            var avatar = App.Avatars.LoadAvatar(user.ID);
            this.AvatarImageBrush.ImageSource = avatar;
            this.CustomAvatarBorder.Visibility = avatar is null ? Visibility.Collapsed : Visibility.Visible;
            this.DefaultAvatarBorder.Visibility = avatar is null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetBusy(bool busy)
        {
            bool enabled = !busy && this.CanModerate;
            this.WarningButton.IsEnabled = enabled;
            this.BlockButton.IsEnabled = enabled;
            this.DeleteButton.IsEnabled = enabled;
        }

        private async Task RunAsync(Func<Task> action)
        {
            this.SetBusy(true);
            try
            {
                await action();
            }
            finally
            {
                this.SetBusy(false);
            }
        }

        private async void WarningButton_Click(object sender, RoutedEventArgs e)
        {
            string? text = TextPromptWindow.Ask(
                this,
                "Попередження користувачу",
                "Текст попередження (користувач побачить його після входу):",
                UserModerationService.MaxWarningLength);
            if (text is null)
            {
                return;
            }

            await this.RunAsync(async () =>
            {
                var result = await this.moderationService.SendWarningAsync(this.targetUserId, text);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Не вдалося надіслати попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Попередження надіслано.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.target is null)
            {
                return;
            }

            if (this.target.IsBlocked)
            {
                await this.RunAsync(async () =>
                {
                    var unblock = await this.moderationService.UnblockUserAsync(this.targetUserId);
                    if (unblock.IsFailure)
                    {
                        MessageBox.Show(unblock.Error, "Не вдалося розблокувати", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await this.LoadAsync();
                });
                return;
            }

            string? reason = TextPromptWindow.Ask(
                this,
                "Блокування акаунта",
                "Причина блокування (необов'язково, її побачить користувач при спробі входу):",
                UserModerationService.MaxReasonLength);
            if (reason is null)
            {
                return;
            }

            await this.RunAsync(async () =>
            {
                var block = await this.moderationService.BlockUserAsync(this.targetUserId, reason);
                if (block.IsFailure)
                {
                    MessageBox.Show(block.Error, "Не вдалося заблокувати", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await this.LoadAsync();
            });
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string name = this.NameTextBlock.Text;
            var confirm = MessageBox.Show(
                $"Видалити акаунт «{name}»? Цю дію неможливо скасувати.\n\nБуде видалено його відгуки та кімнати, власником яких він є. Рецепти залишаться в каталозі без автора.",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            await this.RunAsync(async () =>
            {
                var result = await this.moderationService.DeleteUserAsync(this.targetUserId);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Не вдалося видалити акаунт", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Акаунт видалено.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                this.GoHome();
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.GoHome();
        }

        private void GoHome()
        {
            App.CreateHomeWindow().Show();
            this.Close();
        }
    }
}