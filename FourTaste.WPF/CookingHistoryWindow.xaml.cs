using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class CookingHistoryWindow : Window
    {
        private readonly MealPlanService mealPlanService;

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public CookingHistoryWindow()
            : this(App.ServiceProvider?.GetService<MealPlanService>() ?? throw new InvalidOperationException("MealPlanService не зареєстровано."))
        {
        }

        public CookingHistoryWindow(MealPlanService mealPlanService)
        {
            this.InitializeComponent();
            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateProfileAvatar();
            await this.LoadCookingHistoryAsync();
        }

        private void UpdateProfileAvatar()
        {
            if (this.CurrentUserId <= 0)
            {
                return;
            }

            var avatarImage = AvatarService.LoadAvatar(this.CurrentUserId);
            if (avatarImage != null)
            {
                this.UserAvatarImageBrush.ImageSource = avatarImage;
                this.CustomAvatarBorder.Visibility = Visibility.Visible;
                this.DefaultAvatarBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.UserAvatarImageBrush.ImageSource = null;
                this.CustomAvatarBorder.Visibility = Visibility.Collapsed;
                this.DefaultAvatarBorder.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadCookingHistoryAsync()
        {
            if (this.CurrentUserId <= 0)
            {
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var result = await this.mealPlanService.GetCookingHistoryAsync(this.CurrentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження історії", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cookedMeals = result.Value ?? new List<MealPlan>();
            this.CookingHistoryListBox.ItemsSource = cookedMeals;

            if (cookedMeals.Count == 0)
            {
                this.CookingHistoryListBox.Visibility = Visibility.Collapsed;
                this.EmptyHistoryTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                this.CookingHistoryListBox.Visibility = Visibility.Visible;
                this.EmptyHistoryTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = App.ServiceProvider?.GetService<UserProfileWindow>() ?? new UserProfileWindow();
            profileWindow.Show();
            this.Close();
        }
    }
}
