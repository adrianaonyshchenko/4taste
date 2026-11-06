using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FourTaste.WPF
{
    public partial class RecipeDetailsWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly IAllergyService? _allergyService;
        private readonly ISubstituteService? _substituteService;
        private readonly int _recipeId;

        private Recipe? _currentRecipe;
        private List<Ingredient> _conflictingIngredients = new ();
        private Dictionary<int, AppliedSubstitutionDto> _activeSubstitutions = new ();
        private List<SubstituteOptionDisplayModel> _currentSubstituteOptions = new ();
        private int _selectedIngredientId;

        public RecipeDetailsWindow(IRecipeService recipeService, int recipeId)
            : this(
                recipeService,
                App.ServiceProvider?.GetService<IAllergyService>(),
                App.ServiceProvider?.GetService<ISubstituteService>(),
                recipeId)
        {
        }

        public RecipeDetailsWindow(IRecipeService recipeService, IAllergyService? allergyService, int recipeId)
            : this(
                recipeService,
                allergyService,
                App.ServiceProvider?.GetService<ISubstituteService>(),
                recipeId)
        {
        }

        public RecipeDetailsWindow(
            IRecipeService recipeService,
            IAllergyService? allergyService,
            ISubstituteService? substituteService,
            int recipeId)
        {
            InitializeComponent();
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _allergyService = allergyService;
            _substituteService = substituteService ?? App.ServiceProvider?.GetService<ISubstituteService>();
            _recipeId = recipeId;
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRecipeDetailsAsync();
        }

        private async Task LoadRecipeDetailsAsync()
        {
            int? viewerId = SessionContext.CurrentUser?.ID;

            var result = await _recipeService.GetRecipeDetailsAsync(_recipeId, viewerId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося відкрити рецепт", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }

            var details = result.Value!;
            _currentRecipe = details.Recipe;
            var recipe = _currentRecipe;

            TitleTextBlock.Text = recipe.Title;
            AuthorTextBlock.Text = details.AuthorName;
            TimeTextBlock.Text = $"Час приготування: {recipe.Time}";
            CategoryTextBlock.Text = $"Категорія: {recipe.Category}";
            CuisineTextBlock.Text = $"Країна: {recipe.Cuisine}";
            StatusTextBlock.Text = $"Статус: {(recipe.IsPublic ? "публічний" : "приватний")}";

            int servings = recipe.Servings > 0 ? recipe.Servings : 1;
            ServingsTextBlock.Text = FormatServingsText(servings);

            bool isAuthor = viewerId.HasValue && viewerId.Value == recipe.UserId;
            EditRecipeButton.Visibility = isAuthor ? Visibility.Visible : Visibility.Collapsed;
            EditRecipeButton.IsEnabled = isAuthor;

            _activeSubstitutions.Clear();
            _conflictingIngredients.Clear();
            if (viewerId.HasValue && _substituteService != null)
            {
                var subsTask = _substituteService.GetUserSubstitutionsAsync(viewerId.Value, _recipeId);
                var conflictTask = _substituteService.GetConflictingIngredientsAsync(viewerId.Value, _recipeId);

                await Task.WhenAll(subsTask, conflictTask);

                var subsRes = await subsTask;
                if (subsRes.IsSuccess && subsRes.Value != null)
                {
                    foreach (var sub in subsRes.Value)
                    {
                        _activeSubstitutions[sub.OriginalIngredientId] = sub;
                    }
                }

                var conflictRes = await conflictTask;
                if (conflictRes.IsSuccess && conflictRes.Value != null)
                {
                    _conflictingIngredients = conflictRes.Value.ToList();
                }
            }

            RenderIngredientsList();

            if (!viewerId.HasValue)
            {
                FindSubstituteButton.IsEnabled = false;
                FindSubstituteButton.ToolTip = "Увійдіть до свого облікового запису, щоб бачити замінники для ваших алергенів.";
            }
            else if (_conflictingIngredients.Count == 0)
            {
                FindSubstituteButton.IsEnabled = false;
                FindSubstituteButton.ToolTip = "Алергенів для вашого профілю у цьому рецепті не знайдено.";
            }
            else
            {
                FindSubstituteButton.IsEnabled = true;
                FindSubstituteButton.ToolTip = "Знайти безпечні перевірені замінники для інгредієнтів-алергенів";
            }

            UpdateAllergenSelector();

            StepsItemsControl.ItemsSource = recipe.Steps.OrderBy(s => s.StepNumber).ToList();

            var photo = ToBitmapImage(recipe.Photo);
            if (photo != null)
            {
                RecipeImage.Source = photo;
                PhotoPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecipeImage.Source = null;
                PhotoPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void RenderIngredientsList()
        {
            if (_currentRecipe == null)
            {
                return;
            }

            var conflictingIds = new HashSet<int>(_conflictingIngredients.Select(i => i.Id));

            var ingredientViewModels = _currentRecipe.Ingredients.Select(ing =>
            {
                bool isSubstituted = _activeSubstitutions.TryGetValue(ing.Id, out var sub);
                bool isAllergen = !isSubstituted && conflictingIds.Contains(ing.Id);

                var (cleanName, effectiveAmount) = AllergyMatcher.ExtractCleanNameAndAmount(ing.Name, ing.Amount);

                string displayName = isSubstituted ? sub!.SubstituteIngredientName : cleanName;
                string displayAmount = isSubstituted ? sub!.CalculatedAmount : effectiveAmount;

                return new RecipeIngredientDisplayModel
                {
                    Id = ing.Id,
                    Name = displayName,
                    Amount = displayAmount,
                    IsAllergen = isAllergen,
                    IsSubstituted = isSubstituted,
                    OriginalName = cleanName,
                    OriginalAmount = effectiveAmount,
                    SubstituteNote = isSubstituted ? sub!.Note : null,
                };
            }).ToList();

            IngredientsItemsControl.ItemsSource = ingredientViewModels;
        }

        private void UpdateAllergenSelector()
        {
            if (_conflictingIngredients.Count == 0)
            {
                AllergenicIngredientsComboBox.ItemsSource = null;
                AllergenicIngredientsComboBox.Visibility = Visibility.Collapsed;
                SelectedAllergenNameTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            var selectorItems = _conflictingIngredients.Select(i =>
            {
                var (cleanName, effectiveAmount) = AllergyMatcher.ExtractCleanNameAndAmount(i.Name, i.Amount);
                return new
                {
                    Id = i.Id,
                    DisplayName = string.IsNullOrEmpty(effectiveAmount) ? cleanName : $"{cleanName} ({effectiveAmount})",
                    CleanName = cleanName,
                };
            }).ToList();

            if (_conflictingIngredients.Count == 1)
            {
                AllergenicIngredientsComboBox.Visibility = Visibility.Collapsed;
                SelectedAllergenNameTextBlock.Visibility = Visibility.Visible;
                SelectedAllergenNameTextBlock.Text = selectorItems[0].DisplayName;
                _selectedIngredientId = _conflictingIngredients[0].Id;
            }
            else
            {
                SelectedAllergenNameTextBlock.Visibility = Visibility.Collapsed;
                AllergenicIngredientsComboBox.Visibility = Visibility.Visible;
                AllergenicIngredientsComboBox.DisplayMemberPath = "DisplayName";
                AllergenicIngredientsComboBox.SelectedValuePath = "Id";
                AllergenicIngredientsComboBox.ItemsSource = selectorItems;
                if (AllergenicIngredientsComboBox.SelectedItem == null)
                {
                    AllergenicIngredientsComboBox.SelectedIndex = 0;
                }
            }
        }

        private async void FindSubstituteButton_Click(object sender, RoutedEventArgs e)
        {
            int? viewerId = SessionContext.CurrentUser?.ID;
            if (!viewerId.HasValue)
            {
                MessageBox.Show(
                    "Для отримання безпечних замінників необхідно увійти до системи.",
                    "Потрібна авторизація",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (_conflictingIngredients.Count == 0)
            {
                MessageBox.Show(
                    "У цьому рецепті немає алергенів, що збігаються з вашим профілем.",
                    "Алергенів не знайдено",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            SubstitutePanel.Visibility = Visibility.Visible;

            if (_conflictingIngredients.Count == 1)
            {
                _selectedIngredientId = _conflictingIngredients[0].Id;
            }
            else if (AllergenicIngredientsComboBox.SelectedValue is int selId && selId > 0)
            {
                _selectedIngredientId = selId;
            }
            else
            {
                AllergenicIngredientsComboBox.SelectedIndex = 0;
                _selectedIngredientId = _conflictingIngredients[0].Id;
            }

            await LoadSubstitutesForSelectedIngredientAsync();
        }

        private async void AllergenicIngredientsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AllergenicIngredientsComboBox.SelectedValue is int id && id > 0)
            {
                _selectedIngredientId = id;
                if (SubstitutePanel.Visibility == Visibility.Visible)
                {
                    await LoadSubstitutesForSelectedIngredientAsync();
                }
            }
        }

        private async Task LoadSubstitutesForSelectedIngredientAsync()
        {
            int? viewerId = SessionContext.CurrentUser?.ID;
            if (!viewerId.HasValue || _substituteService == null || _selectedIngredientId <= 0)
            {
                return;
            }

            SubstituteLoadingPanel.Visibility = Visibility.Visible;
            SubstituteErrorPanel.Visibility = Visibility.Collapsed;
            SubstituteEmptyPanel.Visibility = Visibility.Collapsed;
            SubstituteListScrollViewer.Visibility = Visibility.Collapsed;
            SaveSubstitutionButton.IsEnabled = false;

            var result = await _substituteService.GetSafeSubstitutesAsync(viewerId.Value, _recipeId, _selectedIngredientId);

            SubstituteLoadingPanel.Visibility = Visibility.Collapsed;

            if (result.IsFailure)
            {
                SubstituteErrorPanel.Visibility = Visibility.Visible;
                SubstituteErrorTextBlock.Text = result.Error;
                return;
            }

            var safeList = result.Value?.Substitutes ?? new List<SafeSubstituteItemDto>();

            if (safeList.Count == 0)
            {
                SubstituteEmptyPanel.Visibility = Visibility.Visible;
                return;
            }

            _currentSubstituteOptions = safeList.Select(s => new SubstituteOptionDisplayModel
            {
                Id = s.Id,
                Name = s.Name,
                Ratio = s.Ratio,
                Note = s.Note,
                CalculatedAmount = s.CalculatedAmount,
                IsSelected = false,
            }).ToList();

            SubstituteOptionsItemsControl.ItemsSource = _currentSubstituteOptions;
            SubstituteListScrollViewer.Visibility = Visibility.Visible;
        }

        private void SubstituteRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is SubstituteOptionDisplayModel selectedModel)
            {
                foreach (var opt in _currentSubstituteOptions)
                {
                    opt.IsSelected = opt.Id == selectedModel.Id;
                }

                SaveSubstitutionButton.IsEnabled = true;
            }
        }

        private async void SaveSubstitutionButton_Click(object sender, RoutedEventArgs e)
        {
            int? viewerId = SessionContext.CurrentUser?.ID;
            if (!viewerId.HasValue || _substituteService == null)
            {
                MessageBox.Show("Необхідно авторизуватися.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedOption = _currentSubstituteOptions.FirstOrDefault(o => o.IsSelected);
            if (selectedOption == null)
            {
                MessageBox.Show("Будь ласка, оберіть замінник зі списку.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveSubstitutionButton.IsEnabled = false;

            var applyResult = await _substituteService.ApplySubstitutionAsync(
                viewerId.Value,
                _recipeId,
                _selectedIngredientId,
                selectedOption.Id);

            if (applyResult.IsFailure)
            {
                MessageBox.Show(applyResult.Error, "Помилка збереження", MessageBoxButton.OK, MessageBoxImage.Warning);
                SaveSubstitutionButton.IsEnabled = true;
                return;
            }

            _activeSubstitutions[_selectedIngredientId] = applyResult.Value!;
            RenderIngredientsList();

            SubstitutePanel.Visibility = Visibility.Collapsed;

            MessageBox.Show(
                $"Замінник «{selectedOption.Name}» успішно збережено для цього рецепта!",
                "Успіх",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void RevertSubstitutionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.Tag is not RecipeIngredientDisplayModel model)
            {
                return;
            }

            int? viewerId = SessionContext.CurrentUser?.ID;
            if (!viewerId.HasValue || _substituteService == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Повернути оригінальний інгредієнт «{model.OriginalName}»?",
                "Скасування заміни",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var removeResult = await _substituteService.RemoveSubstitutionAsync(viewerId.Value, _recipeId, model.Id);
            if (removeResult.IsFailure)
            {
                MessageBox.Show(removeResult.Error, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _activeSubstitutions.Remove(model.Id);
            RenderIngredientsList();
        }

        private void CloseSubstitutePanelButton_Click(object sender, RoutedEventArgs e)
        {
            SubstitutePanel.Visibility = Visibility.Collapsed;
        }

        private async void RetrySubstituteButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSubstitutesForSelectedIngredientAsync();
        }

        private async void EditRecipeButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditRecipeWindow(_recipeService, _allergyService, _recipeId)
            {
                Owner = this,
            };

            editWindow.ShowDialog();

            if (editWindow.IsDeleted)
            {
                this.Close();
            }
            else if (editWindow.IsSaved)
            {
                await LoadRecipeDetailsAsync();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
