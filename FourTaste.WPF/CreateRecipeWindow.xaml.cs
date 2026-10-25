using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Interfaces;

namespace FourTaste.WPF
{
    public partial class CreateRecipeWindow : Window
    {
        private readonly IRecipeService _recipeService;
        private readonly int _currentUserId;
        private readonly List<string> _ingredientsList = new List<string>();
        private readonly List<string> _stepsList = new List<string>();
        private byte[]? _selectedPhoto;
        public int CreatedRecipeId { get; private set; }

        public CreateRecipeWindow(IRecipeService recipeService, int currentUserId)
        {
            InitializeComponent();
            _recipeService = recipeService;
            _currentUserId = currentUserId;
            IngredientsListBox.ItemsSource = _ingredientsList;
            StepsListBox.ItemsSource = _stepsList;
            CategoryComboBox.ItemsSource = new List<string>
            {
                "Сніданок", "Обід", "Вечеря", "Супи та бульйони", "Основні страви",
                "Салати", "Закуски", "Десерти та випічка", "Напої та коктейлі", "Інше"
            };
            CuisineComboBox.ItemsSource = new List<string>
            {
                "Українська", "Польська", "Італійська", "Японська", "Мексиканська",
                "Французька", "Американська", "Грузинська", "Китайська", "Інша"
            };
            if (AddIngredientButton != null)
            {
                AddIngredientButton.Click += AddIngredientButton_Click;
            }
            if (AddStepButton != null)
            {
                AddStepButton.Click += AddStepButton_Click;
            }
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введіть назву та кількість інгредієнта:", "Додати інгредієнт", " ");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _ingredientsList.Add(input);
                IngredientsListBox.Items.Refresh();
            }
        }

        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введіть опис кроку приготування:", "Додати крок", " ");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _stepsList.Add(input);
                StepsListBox.Items.Refresh();
            }
        }

        private void UploadPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*"
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
                MessageBox.Show("Фото успішно вибрано!", "Фото", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private T FindVisualChild<T>(DependencyObject parent)
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;

            try
            {
                string title = TitleTextBox.Text;
                string time = TimeTextBox.Text;
                string category = CategoryComboBox.Text;
                string cuisine = CuisineComboBox.Text;
                bool isPrivate = StatusCheckBox.IsChecked ?? false;
                bool isPublic = !isPrivate;
                List<string> ingredients = GetIngredientsFromUI();
                List<string> steps = GetStepsFromUI();

                var result = await _recipeService.CreateRecipeAsync(
                    title, time, category, cuisine, isPublic, _currentUserId, ingredients, steps, _selectedPhoto);

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

        private List<string> GetIngredientsFromUI()
        {
            return new List<string>(_ingredientsList);
        }

        private List<string> GetStepsFromUI()
        {
            return new List<string>(_stepsList);
        }
    }
}