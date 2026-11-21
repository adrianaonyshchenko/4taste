using System.Windows;

namespace FourTaste.WPF
{
    public partial class TextPromptWindow : Window
    {
        public TextPromptWindow(string title, string label, int maxLength)
        {
            this.InitializeComponent();
            this.Title = title;
            this.LabelTextBlock.Text = label;
            this.InputTextBox.MaxLength = maxLength;
        }

        public string Value { get; private set; } = string.Empty;

        public static string? Ask(Window owner, string title, string label, int maxLength)
        {
            var window = new TextPromptWindow(title, label, maxLength) { Owner = owner };
            return window.ShowDialog() == true ? window.Value : null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Value = this.InputTextBox.Text.Trim();
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}