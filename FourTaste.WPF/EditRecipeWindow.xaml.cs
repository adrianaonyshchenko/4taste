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
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class EditRecipeWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly IAllergyService? _allergyService;
        private readonly int _recipeId;
        private readonly List<string> _userAllergies = new ();
        private readonly ObservableCollection<RecipeIngredientDisplayModel> _ingredients = new ();
        private readonly ObservableCollection<EditableStepModel> _steps = new ();

        private byte[]? _selectedPhoto;
        private int _servings = 1;

        public EditRecipeWindow(int recipeId)
            : this(
                App.ServiceProvider?.GetService<IRecipeService>() ?? throw new InvalidOperationException("IRecipeService не знайдено."),
                App.ServiceProvider?.GetService<IAllergyService>(),
                recipeId)
        {
        }

        public EditRecipeWindow(IRecipeService recipeService, IAllergyService? allergyService, int recipeId)
        {
            InitializeComponent();
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _allergyService = allergyService;
            _recipeId = recipeId;

            IngredientsItemsControl.ItemsSource = _ingredients;
            StepsItemsControl.ItemsSource = _steps;

            CategoryComboBox.ItemsSource = _recipeService.GetAvailableCategories();
            CuisineComboBox.ItemsSource = _recipeService.GetAvailableCuisines();
        }

        public bool IsSaved { get; private set; }

        public bool IsDeleted { get; private set; }

        private static string FormatServingsText(int count)
        {
            int mod10 = count % 10;
            int mod100 = count % 100;

            if (mod10 == 1 && mod100 != 11)
            {
                return $"{count}\nпорція";
            }

            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20))
            {
                return $"{count}\nпорції";
            }

            return $"{count}\nпорцій";
        }

        private static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int? currentUserId = SessionContext.CurrentUser?.ID;
            if (currentUserId is null || currentUserId <= 0)
            {
                MessageBox.Show("Будь ласка, увійдіть у систему для редагування рецепта.", "Помилка авторизації", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            // Отримуємо рецепт
            var result = await _recipeService.GetRecipeDetailsAsync(_recipeId, currentUserId);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            var recipe = result.Value!.Recipe;

            // Перевірка авторизації: лише автор може відкрити редагування
            if (recipe.UserId != currentUserId.Value)
            {
                MessageBox.Show("Ви можете редагувати виключно власні рецепти.", "Доступ заборонено", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            // Завантажуємо алергени користувача
            if (_allergyService != null)
            {
                var allergiesResult = await _allergyService.GetAllergiesByUserIdAsync(currentUserId.Value);
                if (allergiesResult.IsSuccess && allergiesResult.Value != null)
                {
                    _userAllergies.Clear();
                    _userAllergies.AddRange(allergiesResult.Value
                        .Where(a => !string.IsNullOrWhiteSpace(a.AllergenName))
                        .Select(a => a.AllergenName.Trim().ToLower()));
                }
            }

            // Заповнення полів даними рецепта
            TitleTextBox.Text = recipe.Title;
            TimeTextBox.Text = string.IsNullOrWhiteSpace(recipe.Time) ? "00:00" : recipe.Time;

            CategoryComboBox.SelectedItem = recipe.Category;
            if (CategoryComboBox.SelectedItem == null && !string.IsNullOrWhiteSpace(recipe.Category))
            {
                CategoryComboBox.Text = recipe.Category;
            }

            CuisineComboBox.SelectedItem = recipe.Cuisine;
            if (CuisineComboBox.SelectedItem == null && !string.IsNullOrWhiteSpace(recipe.Cuisine))
            {
                CuisineComboBox.Text = recipe.Cuisine;
            }

            StatusComboBox.SelectedIndex = recipe.IsPublic ? 1 : 0;

            _servings = recipe.Servings > 0 ? recipe.Servings : 1;
            UpdateServingsDisplay();

            // Фото
            _selectedPhoto = recipe.Photo;
            var photoBitmap = ToBitmapImage(_selectedPhoto);
            if (photoBitmap != null)
            {
                RecipeImage.Source = photoBitmap;
                PhotoPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecipeImage.Source = null;
                PhotoPlaceholder.Visibility = Visibility.Visible;
            }

            // Інгредієнти
            _ingredients.Clear();
            foreach (var ing in recipe.Ingredients)
            {
                _ingredients.Add(CreateIngredientDisplayModel(ing.Name, ing.Amount));
            }

            // Кроки
            _steps.Clear();
            int stepNum = 1;
            foreach (var step in recipe.Steps.OrderBy(s => s.StepNumber))
            {
                _steps.Add(new EditableStepModel
                {
                    StepNumber = stepNum++,
                    Description = step.Description,
                });
            }

            if (_steps.Count == 0)
            {
                _steps.Add(new EditableStepModel { StepNumber = 1, Description = string.Empty });
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

        private void UpdateServingsDisplay()
        {
            ServingsTextBlock.Text = FormatServingsText(_servings);
        }

        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TitlePlaceholder != null)
            {
                TitlePlaceholder.Visibility = string.IsNullOrWhiteSpace(TitleTextBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Оберіть фото рецепта",
                Filter = "Зображення (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png|Всі файли (*.*)|*.*",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bytes = File.ReadAllBytes(openFileDialog.FileName);
                    if (bytes.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Розмір фотографії не може перевищувати 5 МБ.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedPhoto = bytes;
                    var bitmap = ToBitmapImage(_selectedPhoto);
                    RecipeImage.Source = bitmap;
                    PhotoPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не вдалося завантажити зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangePortionsButton_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Введіть бажану кількість порцій:",
                "Зміна порцій",
                _servings.ToString());

            if (int.TryParse(input, out int newServings) && newServings > 0)
            {
                _servings = newServings;
                UpdateServingsDisplay();
            }
            else if (!string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Кількість порцій має бути цілим додатним числом.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            _steps.Add(new EditableStepModel
            {
                StepNumber = _steps.Count + 1,
                Description = string.Empty,
            });
        }

        private void DeleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is EditableStepModel step)
            {
                _steps.Remove(step);
                int num = 1;
                foreach (var s in _steps)
                {
                    s.StepNumber = num++;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int? currentUserId = SessionContext.CurrentUser?.ID;
            if (currentUserId is null || currentUserId <= 0)
            {
                MessageBox.Show("Необхідно авторизуватися.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = TitleTextBox.Text?.Trim() ?? string.Empty;
            string time = TimeTextBox.Text?.Trim() ?? string.Empty;
            string category = CategoryComboBox.SelectedItem?.ToString() ?? CategoryComboBox.Text?.Trim() ?? string.Empty;
            string cuisine = CuisineComboBox.SelectedItem?.ToString() ?? CuisineComboBox.Text?.Trim() ?? string.Empty;
            bool isPublic = StatusComboBox.SelectedIndex == 1;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Назва рецепта не може бути порожньою.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Оберіть категорію рецепта.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(cuisine))
            {
                MessageBox.Show("Оберіть країну/кухню рецепта.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                CuisineComboBox.Focus();
                return;
            }

            var ingredientsList = _ingredients
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .Select(i => new RecipeIngredientDto
                {
                    Name = i.Name.Trim(),
                    Amount = i.Amount.Trim(),
                })
                .ToList();

            if (ingredientsList.Count == 0)
            {
                MessageBox.Show("Додайте хоча б один інгредієнт.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ingredientsList.Any(i => string.IsNullOrWhiteSpace(i.Amount)))
            {
                MessageBox.Show("Вкажіть кількість для кожного інгредієнта.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var stepsList = _steps
                .Where(s => !string.IsNullOrWhiteSpace(s.Description))
                .Select(s => s.Description.Trim())
                .ToList();

            if (stepsList.Count == 0)
            {
                MessageBox.Show("Додайте кроки приготування.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveButton.IsEnabled = false;

            try
            {
                var request = new UpdateRecipeRequest
                {
                    Id = _recipeId,
                    UserId = currentUserId.Value,
                    Title = title,
                    Time = time,
                    Category = category,
                    Cuisine = cuisine,
                    IsPublic = isPublic,
                    Servings = _servings,
                    Photo = _selectedPhoto,
                    UpdatePhoto = true,
                    Ingredients = ingredientsList,
                    Steps = stepsList,
                };

                var result = await _recipeService.UpdateRecipeAsync(request);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsSaved = true;
                MessageBox.Show("Рецепт успішно збережено!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int? currentUserId = SessionContext.CurrentUser?.ID;
            if (currentUserId is null || currentUserId <= 0)
            {
                MessageBox.Show("Необхідно авторизуватися.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = TitleTextBox.Text?.Trim() ?? "цей рецепт";
            var confirm = MessageBox.Show(
                $"Ви дійсно бажаєте видалити рецепт «{title}»?",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            DeleteButton.IsEnabled = false;

            try
            {
                var result = await _recipeService.DeleteRecipeAsync(_recipeId, currentUserId.Value);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsDeleted = true;
                MessageBox.Show("Рецепт успішно видалено з каталогу.", "Видалення", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            finally
            {
                DeleteButton.IsEnabled = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
