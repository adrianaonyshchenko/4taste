using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.WPF
{
    public partial class EditIngredientsWindow : Window
    {
        public ObservableCollection<EditableIngredientModel> Ingredients { get; } = new ();

        public EditIngredientsWindow(IEnumerable<RecipeIngredientDto> initialIngredients)
        {
            InitializeComponent();

            foreach (var item in initialIngredients)
            {
                Ingredients.Add(new EditableIngredientModel
                {
                    Name = item.Name,
                    Amount = item.Amount,
                });
            }

            IngredientsItemsControl.ItemsSource = Ingredients;
        }

        public List<RecipeIngredientDto> GetResultIngredients()
        {
            return Ingredients
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .Select(i => new RecipeIngredientDto
                {
                    Name = i.Name.Trim(),
                    Amount = i.Amount.Trim(),
                })
                .ToList();
        }

        private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NewIngredientNameTextBox.Text?.Trim() ?? string.Empty;
            string amount = NewIngredientAmountTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть назву інгредієнта.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewIngredientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(amount))
            {
                MessageBox.Show("Вкажіть кількість для інгредієнта.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewIngredientAmountTextBox.Focus();
                return;
            }

            Ingredients.Add(new EditableIngredientModel
            {
                Name = name,
                Amount = amount,
            });

            NewIngredientNameTextBox.Text = string.Empty;
            NewIngredientAmountTextBox.Text = string.Empty;
            NewIngredientNameTextBox.Focus();
        }

        private void DeleteIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is EditableIngredientModel item)
            {
                Ingredients.Remove(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var valid = Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
            if (valid.Count == 0)
            {
                MessageBox.Show("Додайте хоча б один інгредієнт.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (valid.Any(i => string.IsNullOrWhiteSpace(i.Amount)))
            {
                MessageBox.Show("Вкажіть кількість для кожного інгредієнта.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
