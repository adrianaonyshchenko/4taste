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
    public partial class MealPlanWindow : Window
    {
        private readonly MealPlanService mealPlanService;
        private readonly IRecipeService recipeService;
        private List<Recipe> availableRecipes = new ();

        private int CurrentUserId => SessionContext.CurrentUser?.ID ?? 0;

        public MealPlanWindow(MealPlanService mealPlanService)
            : this(mealPlanService, App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService is not initialized."))
        {
        }

        public MealPlanWindow(MealPlanService mealPlanService, IRecipeService recipeService)
        {
            this.InitializeComponent();
            this.mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));

            this.MenuCalendar.SelectedDate = DateTime.Today;
            _ = LoadRecipesAsync();
        }

        private async Task LoadRecipesAsync()
        {
            var criteria = new RecipeSearchCriteria();
            var recipesResult = await this.recipeService.SearchRecipesAsync(criteria);

            this.availableRecipes = recipesResult.IsSuccess
                ? recipesResult.Value?.Where(r => r.UserId == CurrentUserId || r.IsPublic).ToList() ?? new List<Recipe>()
                : new List<Recipe>();

            this.RecipeComboBox.ItemsSource = this.availableRecipes;
        }

        private async void CreateRecipeFastButton_Click(object sender, RoutedEventArgs e)
        {
            var createWin = new CreateRecipeWindow(this.recipeService, this.CurrentUserId);
            if (createWin.ShowDialog() == true)
            {
                await LoadRecipesAsync();
                this.RecipeComboBox.SelectedValue = createWin.CreatedRecipeId;
            }
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

            int? selectedRecipeId = this.RecipeComboBox.SelectedValue as int?;
            string recipeName = this.RecipeComboBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(recipeName) && this.RecipeComboBox.SelectedItem is Recipe selRec)
            {
                recipeName = selRec.Title;
            }
            else if (selectedRecipeId == null && !string.IsNullOrEmpty(recipeName))
            {
                var matched = this.availableRecipes.FirstOrDefault(r => r.Title.Equals(recipeName, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    selectedRecipeId = matched.Id;
                }
            }

            if (string.IsNullOrEmpty(recipeName))
            {
                MessageBox.Show(this, "Будь ласка, введіть назву страви або виберіть рецепт.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime selectedDate = this.MenuCalendar.SelectedDate.Value;

            await this.mealPlanService.AddCustomRecipeToPlanAsync(this.CurrentUserId, recipeName, selectedRecipeId, selectedDate);

            this.RecipeComboBox.Text = string.Empty;
            this.RecipeComboBox.SelectedIndex = -1;
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