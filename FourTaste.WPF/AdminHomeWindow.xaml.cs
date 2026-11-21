using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class AdminHomeWindow : Window
    {
        private const int SearchDelayMs = 300;

        private readonly IAdminService adminService;
        private int searchVersion;

        public AdminHomeWindow()
            : this(App.ServiceProvider?.GetService<IAdminService>() ?? throw new InvalidOperationException("IAdminService не зареєстровано."))
        {
        }

        public AdminHomeWindow(IAdminService adminService)
        {
            this.InitializeComponent();
            this.adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        }

        private RadioButton[] FilterRadios => new[] { this.FilterAllRadio, this.FilterNameRadio, this.FilterUsernameRadio, this.FilterEmailRadio };

        private UserSearchFilter CurrentFilter =>
            this.FilterRadios.Where(r => r.IsChecked == true).Select(r => (UserSearchFilter)r.Tag).FirstOrDefault();

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.CurrentUser.IsAdmin)
            {
                MessageBox.Show("Ця сторінка доступна лише адміністраторам.", "Недостатньо прав", MessageBoxButton.OK, MessageBoxImage.Warning);
                App.CreateHomeWindow().Show();
                this.Close();
                return;
            }

            this.UpdateSearchControls();
            this.UpdateProfileAvatar();
            await this.SearchAsync(immediate: true);
        }

        private void UpdateProfileAvatar()
        {
            var avatar = App.Avatars.LoadAvatar(App.CurrentUser.UserId);
            this.UserAvatarImageBrush.ImageSource = avatar;
            this.CustomAvatarBorder.Visibility = avatar is null ? Visibility.Collapsed : Visibility.Visible;
            this.DefaultAvatarBorder.Visibility = avatar is null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSearchControls()
        {
            bool hasText = this.SearchTextBox.Text.Length > 0;
            this.SearchPlaceholderTextBlock.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            this.ClearSearchButton.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task SearchAsync(bool immediate)
        {
            string query = this.SearchTextBox.Text.Trim();
            int version = ++this.searchVersion;

            if (!immediate)
            {
                await Task.Delay(SearchDelayMs);
            }

            if (version != this.searchVersion)
            {
                return;
            }

            var result = await this.adminService.SearchUsersAsync(query, this.CurrentFilter);
            if (version != this.searchVersion)
            {
                return;
            }

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var users = result.Value!;
            var cards = await Task.Run(() => users.Select(u => new UserCardViewModel(u)).ToList());
            this.ShowUsers(cards, query);
        }

        private void ShowUsers(List<UserCardViewModel> cards, string query)
        {
            this.ResultsCountTextBlock.Text = $"Знайдено: {cards.Count}";
            this.UsersItemsControl.ItemsSource = cards;

            bool isEmpty = cards.Count == 0;
            this.EmptyTextBlock.Text = query.Length == 0
                ? "Поки немає жодного зареєстрованого користувача."
                : $"За запитом «{query}» користувачів не знайдено.";
            this.EmptyTextBlock.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            this.UsersItemsControl.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateSearchControls();

            if (this.IsLoaded)
            {
                await this.SearchAsync(immediate: false);
            }
        }

        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await this.SearchAsync(immediate: true);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                await this.ClearSearchAsync();
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await this.SearchAsync(immediate: true);
        }

        private async void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ClearSearchAsync();
        }

        private async Task ClearSearchAsync()
        {
            this.SearchTextBox.Text = string.Empty;
            this.SearchTextBox.Focus();
            await this.SearchAsync(immediate: true);
        }

        private async void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                await this.SearchAsync(immediate: true);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            App.CreateProfileWindow().Show();
            this.Close();
        }

        private void UserCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: UserCardViewModel card })
            {
                new AdminUserDetailsWindow(card.Id).Show();
                this.Close();
            }
        }
    }
}
