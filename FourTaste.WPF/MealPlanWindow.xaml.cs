using System;
using System.Windows;
using System.Windows.Controls;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Services;

namespace FourTaste.WPF
{
    public partial class MealPlanWindow : Window
    {
        private readonly MealPlanService mealPlanService;
        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public MealPlanWindow(MealPlanService mealPlanService)
        {
            this.InitializeComponent();
            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.MenuCalendar.SelectedDate = DateTime.Today;
        }

        private async void MenuCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.MenuCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;
                this.SelectedDateTextBlock.Text = selectedDate.ToString("d MMMM");

                if (this.CurrentUserId > 0)
                {
                    var plans = await this.mealPlanService.GetUserPlansForDateAsync(this.CurrentUserId, selectedDate);
                    this.PlannedRecipesListBox.ItemsSource = plans;
                }
            }
        }

        private async void AddRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.MenuCalendar.SelectedDate.HasValue || this.CurrentUserId <= 0)
            {
                return;
            }

            string recipeName = this.RecipeTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(recipeName))
            {
                return;
            }

            DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;
            await this.mealPlanService.AddCustomRecipeToPlanAsync(this.CurrentUserId, recipeName, selectedDate);

            this.RecipeTextBox.Clear();
            this.PlannedRecipesListBox.ItemsSource = await this.mealPlanService.GetUserPlansForDateAsync(this.CurrentUserId, selectedDate);
        }

        private async void DeleteRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.MenuCalendar.SelectedDate.HasValue || this.CurrentUserId <= 0)
            {
                return;
            }

            if (this.PlannedRecipesListBox.SelectedItem is MealPlan selectedMealPlan)
            {
                await this.mealPlanService.DeleteMealPlanAsync(selectedMealPlan.Id, this.CurrentUserId);
                DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;
                this.PlannedRecipesListBox.ItemsSource = await this.mealPlanService.GetUserPlansForDateAsync(this.CurrentUserId, selectedDate);
            }
        }
        private async void RecipeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is MealPlan mealPlan && this.CurrentUserId > 0)
            {
                bool newStatus = checkBox.IsChecked ?? false;
                await this.mealPlanService.UpdateMealPlanStatusAsync(mealPlan.Id, this.CurrentUserId, newStatus);
                mealPlan.IsCooked = newStatus;
            }
        }
    }
}