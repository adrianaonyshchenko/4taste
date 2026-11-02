using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class CreateRecipeWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly IAllergyService? _allergyService;
        private readonly int _currentUserId;
        private readonly List<string> _userAllergies = new ();
        private readonly ObservableCollection<RecipeIngredientDisplayModel> _ingredients = new ();
        private readonly List<string> _stepsList = new ();

        private byte[]? _selectedPhoto;

        public CreateRecipeWindow(IRecipeService recipeService, int currentUserId)
            : this(recipeService, App.ServiceProvider?.GetService<IAllergyService>(), currentUserId)
        {
        }

        public CreateRecipeWindow(IRecipeService recipeService, IAllergyService? allergyService, int currentUserId)
        {
            InitializeComponent();
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _allergyService = allergyService;
            _currentUserId = currentUserId;

            IngredientsItemsControl.ItemsSource = _ingredients;
            StepsListBox.ItemsSource = _stepsList;

            CategoryComboBox.ItemsSource = RecipeOptions.Categories;
            CuisineComboBox.ItemsSource = RecipeOptions.Cuisines;

            if (AddStepButton != null)
            {
                AddStepButton.Click += AddStepButton_Click;
            }
        }

        public int CreatedRecipeId { get; private set; }

        private static T FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null!;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_allergyService != null && _currentUserId > 0)
            {
                var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(_currentUserId);
                if (allergiesResult.IsSuccess && allergiesResult.Value != null)
                {
                    _userAllergies.Clear();
                    _userAllergies.AddRange(allergiesResult.Value
                        .Where(a => !string.IsNullOrWhiteSpace(a.AllergenName))
                        .Select(a => a.AllergenName.Trim().ToLower()));
                }
            }
        }

        private RecipeIngredientDisplayModel CreateIngredientDisplayModel(string name, string amount)
        {
            string trimmedName = name?.Trim() ?? string.Empty;
            string trimmedAmount = string.IsNullOrWhiteSpace(amount) ? "1 шт." : amount.Trim();

            bool isAllergen = _userAllergies.Any(allergen =>
                !string.IsNullOrWhiteSpace(allergen) &&
                trimmedName.ToLower().Contains(allergen));

            return new RecipeIngredientDisplayModel
            {
                Name = trimmedName,
                Amount = trimmedAmount,
                IsAllergen = isAllergen,
            };
        }

        private void ChangeIngredientsButton_Click(object sender, RoutedEventArgs e)
        {
            var initialList = _ingredients.Select(i => new RecipeIngredientDto
            {
                Name = i.Name,
                Amount = i.Amount,
            }).ToList();

            var dialog = new EditIngredientsWindow(initialList)
            {
                Owner = this,
            };

            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetResultIngredients();
                _ingredients.Clear();
                foreach (var item in updated)
                {
                    _ingredients.Add(CreateIngredientDisplayModel(item.Name, item.Amount));
                }
            }
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введіть опис кроку приготування:", "Додати крок", " ");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _stepsList.Add(input.Trim());
                StepsListBox.Items.Refresh();
            }
        }

        private void UploadPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Оберіть фото рецепта",
                Filter = "Зображення (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png|Всі файли (*.*)|*.*",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedPhoto = File.ReadAllBytes(openFileDialog.FileName);
                var template = (Button)sender;
                var imageChild = FindVisualChild<Image>(template);
                if (imageChild != null)
                {
                    imageChild.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                }

                var placeholder = FindVisualChild<TextBlock>(template);
                if (placeholder != null)
                {
                    placeholder.Visibility = Visibility.Collapsed;
                }

                MessageBox.Show("Фото успішно вибрано!", "Фото", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;

            try
            {
                string title = TitleTextBox.Text?.Trim() ?? string.Empty;
                string time = TimeTextBox.Text?.Trim() ?? string.Empty;
                string category = CategoryComboBox.SelectedItem?.ToString() ?? CategoryComboBox.Text?.Trim() ?? string.Empty;
                string cuisine = CuisineComboBox.SelectedItem?.ToString() ?? CuisineComboBox.Text?.Trim() ?? string.Empty;
                bool isPrivate = StatusCheckBox.IsChecked ?? false;
                bool isPublic = !isPrivate;

                var ingredientItems = _ingredients
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                    .Select(i => new RecipeIngredientDto
                    {
                        Name = i.Name.Trim(),
                        Amount = i.Amount.Trim(),
                    })
                    .ToList();

                List<string> steps = GetStepsFromUI();

                var request = new CreateRecipeRequest
                {
                    Title = title,
                    Time = time,
                    Category = category,
                    Cuisine = cuisine,
                    IsPublic = isPublic,
                    UserId = _currentUserId,
                    IngredientItems = ingredientItems,
                    Ingredients = ingredientItems.Select(i => $"{i.Name} {i.Amount}".Trim()).ToList(),
                    Steps = steps,
                    Photo = _selectedPhoto,
                };

                var result = await _recipeService.CreateRecipeAsync(request);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CreatedRecipeId = result.Value;
                MessageBox.Show("Рецепт успішно створено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            finally
            {
                if (SaveButton != null)
                {
                    SaveButton.IsEnabled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private List<string> GetStepsFromUI()
        {
            return new List<string>(_stepsList);
        }
    }
}
