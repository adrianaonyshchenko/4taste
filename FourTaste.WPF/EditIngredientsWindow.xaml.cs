using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.WPF
{
    public partial class EditIngredientsWindow : Window
    {
        public ObservableCollection<EditableIngredientModel> Ingredients { get; } = new ();

        public EditIngredientsWindow(IEnumerable<RecipeIngredientDto> initialIngredients)
        {
            this.InitializeComponent();

            foreach (var item in initialIngredients)
            {
                this.Ingredients.Add(new EditableIngredientModel
                {
                    Name = item.Name,
                    Amount = item.Amount,
                });
            }

            this.IngredientsItemsControl.ItemsSource = this.Ingredients;
        }

        public List<RecipeIngredientDto> GetResultIngredients()
        {
            return this.Ingredients
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
            string name = this.NewIngredientNameTextBox.Text?.Trim() ?? string.Empty;
            string amount = this.NewIngredientAmountTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введіть назву інгредієнта.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.NewIngredientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(amount))
            {
                MessageBox.Show("Вкажіть кількість для інгредієнта.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.NewIngredientAmountTextBox.Focus();
                return;
            }

            this.Ingredients.Add(new EditableIngredientModel
            {
                Name = name,
                Amount = amount,
            });

            this.NewIngredientNameTextBox.Text = string.Empty;
            this.NewIngredientAmountTextBox.Text = string.Empty;
            this.NewIngredientNameTextBox.Focus();
        }

        private void NewIngredient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (sender == this.NewIngredientNameTextBox && string.IsNullOrWhiteSpace(this.NewIngredientAmountTextBox.Text))
                {
                    this.NewIngredientAmountTextBox.Focus();
                }
                else
                {
                    this.AddIngredientButton_Click(this.AddIngredientButton, new RoutedEventArgs());
                }
            }
        }

        private void DeleteIngredientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is EditableIngredientModel item)
            {
                this.Ingredients.Remove(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var valid = this.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
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

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
