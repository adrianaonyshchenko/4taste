using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FourTaste.WPF
{
    public class SubstituteOptionDisplayModel : INotifyPropertyChanged
    {
        private bool isSelected;

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Ratio { get; set; } = 1.0;

        public string? Note { get; set; }

        public string CalculatedAmount { get; set; } = string.Empty;

        public string DisplayDetails => $"Кількість: {CalculatedAmount}";

        public bool HasNote => !string.IsNullOrWhiteSpace(Note);

        public System.Windows.Visibility NoteVisibility => this.HasNote ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
