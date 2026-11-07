using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class CookingStepWindow : Window
    {
        private readonly Recipe _recipe;
        private readonly MealPlanService? _mealPlanService;
        private int _currentIndex = 0;
        private bool _isWhyVisible = false;
        private double _currentFontSize = 16.0;

        private DispatcherTimer? _timer;
        private TimeSpan _remainingTime = TimeSpan.Zero;
        private bool _isTimerRunning = false;

        private class StepParsedData
        {
            public string Description { get; set; } = string.Empty;
            public string Why { get; set; } = string.Empty;
        }

        private readonly List<StepParsedData> _parsedSteps = new ();

        public CookingStepWindow(Recipe recipe, List<RecipeIngredientDisplayModel>? ingredients = null)
        {
            this.InitializeComponent();
            this._recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            this._mealPlanService = App.ServiceProvider?.GetService<MealPlanService>();

            if (ingredients != null)
            {
                this.IngredientsItemsControl.ItemsSource = ingredients;
            }

            this.LoadPhoto();
            this.ParseSteps();
            this.DisplayCurrentStep();
            this.InitializeTimer();
        }

        private void LoadPhoto()
        {
            if (this._recipe.Photo == null || this._recipe.Photo.Length == 0)
            {
                this.StepImageViewer.Source = null;
                return;
            }

            using var stream = new MemoryStream(this._recipe.Photo);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            this.StepImageViewer.Source = image;
        }

        private void ParseSteps()
        {
            this._parsedSteps.Clear();
            if (this._recipe.Steps == null)
            {
                return;
            }

            foreach (var step in this._recipe.Steps)
            {
                string text = step.Description ?? string.Empty;
                string why = string.Empty;

                int whyIndex = text.IndexOf("|WHY:", StringComparison.Ordinal);
                if (whyIndex >= 0)
                {
                    why = text.Substring(whyIndex + 5).Trim();
                    text = text.Substring(0, whyIndex).Trim();
                }

                this._parsedSteps.Add(new StepParsedData
                {
                    Description = text,
                    Why = why,
                });
            }
        }

        private void DisplayCurrentStep()
        {
            if (this._parsedSteps.Count == 0)
            {
                MessageBox.Show("У цьому рецепті немає кроків.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }

            this.RecipeTitleTextBlock.Text = !string.IsNullOrWhiteSpace(this._recipe.Title) ? this._recipe.Title : "Рецепт";

            this.StepNumberTextBlock.Text = $"Крок {this._currentIndex + 1} з {this._parsedSteps.Count}";
            this.StepDescriptionTextBlock.Text = this._parsedSteps[this._currentIndex].Description;

            string whyText = this._parsedSteps[this._currentIndex].Why;
            this.WhyExplanationTextBlock.Text = string.IsNullOrEmpty(whyText) ? "Для цього кроку додаткове пояснення не вказано." : whyText;

            this.WhyButton.Visibility = string.IsNullOrEmpty(whyText) ? Visibility.Collapsed : Visibility.Visible;

            this.WhyContainerBorder.Visibility = Visibility.Collapsed;
            this._isWhyVisible = false;

            if (this._currentIndex == this._parsedSteps.Count - 1)
            {
                this.NextButton.Content = "Готово";
            }
            else
            {
                this.NextButton.Content = "Далі";
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._currentIndex < this._parsedSteps.Count - 1)
            {
                this._currentIndex++;
                this.DisplayCurrentStep();
            }
            else
            {
                this.StopTimer();

                int userId = SessionContext.CurrentUser?.ID ?? 0;
                if (userId > 0 && this._mealPlanService != null)
                {
                    string recipeTitle = !string.IsNullOrWhiteSpace(this._recipe.Title) ? this._recipe.Title : "Рецепт";

                    var mealPlan = new MealPlan
                    {
                        UserId = userId,
                        RecipeId = this._recipe.Id,
                        RecipeName = recipeTitle,
                        PlannedDate = DateTime.Today,
                        IsCooked = true,
                    };

                    var result = await this._mealPlanService.RecordCookedRecipeAsync(userId, recipeTitle, DateTime.Today);
                    if (result.IsFailure)
                    {
                        MessageBox.Show($"Не вдалося зберегти в історію: {result.Error}", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                MessageBox.Show("Вітаємо! Рецепт успішно приготовано та додано до історії!", "Смачного!", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
        }

        private void WhyButton_Click(object sender, RoutedEventArgs e)
        {
            this._isWhyVisible = !this._isWhyVisible;
            this.WhyContainerBorder.Visibility = this._isWhyVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void IncreaseTextSize_Click(object sender, RoutedEventArgs e)
        {
            this._currentFontSize = this._currentFontSize >= 24.0 ? 16.0 : this._currentFontSize + 4.0;
            this.StepDescriptionTextBlock.FontSize = this._currentFontSize;
        }

        private void InitializeTimer()
        {
            this._timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            this._timer.Tick += this.Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (this._remainingTime.TotalSeconds > 0)
            {
                this._remainingTime = this._remainingTime.Subtract(TimeSpan.FromSeconds(1));
                this.TimerInputTextBox.Text = this._remainingTime.ToString(@"mm\:ss");
            }
            else
            {
                this.StopTimer();
                MessageBox.Show("Час таймера вийшов!", "Таймер", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._isTimerRunning)
            {
                this.StopTimer();
                this.TimerButton.Content = "Таймер";
            }
            else
            {
                if (TimeSpan.TryParseExact(this.TimerInputTextBox.Text.Trim(), @"mm\:ss", null, out var parsedTime) && parsedTime > TimeSpan.Zero)
                {
                    this._remainingTime = parsedTime;
                    this._timer?.Start();
                    this._isTimerRunning = true;
                    this.TimerButton.Content = "Стоп";
                }
                else
                {
                    MessageBox.Show("Введіть коректний час у форматі ММ:СС (наприклад, 05:00)", "Помилка таймера", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void StopTimer()
        {
            this._timer?.Stop();
            this._isTimerRunning = false;
            this.TimerButton.Content = "Таймер";
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.StopTimer();
            this.Close();
        }
    }
}