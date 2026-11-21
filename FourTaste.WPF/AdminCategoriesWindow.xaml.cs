using FourTaste.ApplicationCore.Interfaces;
using FourTaste.WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FourTaste.WPF
{
    public partial class AdminCategoriesWindow : Window
    {
        private readonly IRecipeCategoryService categoryService;
        private readonly IRecipeCategoryRepository categoryRepository;

        public AdminCategoriesWindow()
            : this(
                App.ServiceProvider?.GetService<IRecipeCategoryService>() ?? throw new InvalidOperationException("IRecipeCategoryService не зареєстровано."),
                App.ServiceProvider?.GetService<IRecipeCategoryRepository>() ?? throw new InvalidOperationException("IRecipeCategoryRepository не зареєстровано."))
        {
        }

        public AdminCategoriesWindow(IRecipeCategoryService categoryService, IRecipeCategoryRepository categoryRepository)
        {
            this.InitializeComponent();
            this.categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.CurrentUser.IsAdmin)
            {
                MessageBox.Show("Ця сторінка доступна лише адміністраторам.", "Недостатньо прав", MessageBoxButton.OK, MessageBoxImage.Warning);
                App.CreateHomeWindow().Show();
                this.Close();
                return;
            }

            await this.LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            var result = await this.categoryService.GetAllAsync();
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Помилка завантаження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var items = new List<CategoryItemViewModel>();
            foreach (var category in result.Value!)
            {
                int recipesCount = await this.categoryRepository.CountRecipesByCategoryNameAsync(category.Name);
                items.Add(new CategoryItemViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    RecipesCount = recipesCount,
                    RecipesCountText = $"Рецептів у категорії: {recipesCount}"
                });
            }

            this.CountTextBlock.Text = $"Усього категорій: {items.Count}";
            this.CategoriesItemsControl.ItemsSource = items;

            bool isEmpty = items.Count == 0;
            this.EmptyTextBlock.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            this.CategoriesItemsControl.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            string? input = this.PromptForName(
                title: "Нова категорія",
                prompt: "Введіть назву нової категорії рецептів:",
                initialValue: string.Empty);

            if (input is null)
            {
                return;
            }

            this.AddCategoryButton.IsEnabled = false;
            var result = await this.categoryService.CreateAsync(input);
            this.AddCategoryButton.IsEnabled = true;

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося створити категорію", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadCategoriesAsync();
        }

        private async void RenameCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: CategoryItemViewModel item })
            {
                return;
            }

            string? input = this.PromptForName(
                title: "Перейменування категорії",
                prompt: $"Нова назва для категорії «{item.Name}»:",
                initialValue: item.Name);

            if (input is null)
            {
                return;
            }

            var result = await this.categoryService.RenameAsync(item.Id, input);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося перейменувати", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadCategoriesAsync();
        }

        private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: CategoryItemViewModel item })
            {
                return;
            }

            var confirm = MessageBox.Show(
                $"Видалити категорію «{item.Name}»?",
                "Підтвердження видалення",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = await this.categoryService.DeleteAsync(item.Id);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error, "Не вдалося видалити категорію", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await this.LoadCategoriesAsync();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var adminHome = App.ServiceProvider?.GetService<AdminHomeWindow>() ?? new AdminHomeWindow();
            adminHome.Show();
            this.Close();
        }

        private string? PromptForName(string title, string prompt, string initialValue)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Transparent
            };

            var rootBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF4, 0xEA, 0xE0)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xC4, 0xB4, 0xA4)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20),
                Margin = new Thickness(8)
            };

            var stack = new StackPanel();
            var label = new TextBlock
            {
                Text = prompt,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4A, 0x35, 0x32)),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox
            {
                Text = initialValue,
                FontSize = 14,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "Скасувати",
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 8, 0),
                IsCancel = true
            };

            var okButton = new Button
            {
                Content = "Зберегти",
                Padding = new Thickness(16, 6, 16, 6),
                FontWeight = FontWeights.SemiBold,
                IsDefault = true
            };

            okButton.Click += (_, _) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };

            buttonsPanel.Children.Add(cancelButton);
            buttonsPanel.Children.Add(okButton);

            stack.Children.Add(label);
            stack.Children.Add(textBox);
            stack.Children.Add(buttonsPanel);
            rootBorder.Child = stack;
            dialog.Content = rootBorder;

            dialog.Loaded += (_, _) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            bool? accepted = dialog.ShowDialog();
            return accepted == true ? textBox.Text : null;
        }

        private sealed class CategoryItemViewModel
        {
            public int Id { get; init; }

            public string Name { get; init; } = string.Empty;

            public int RecipesCount { get; init; }

            public string RecipesCountText { get; init; } = string.Empty;
        }
    }
}
