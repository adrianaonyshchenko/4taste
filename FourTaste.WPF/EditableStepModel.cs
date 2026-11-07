using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FourTaste.WPF
{
    public class EditableStepModel : INotifyPropertyChanged
    {
        private int _stepNumber;
        private string _description = string.Empty;
        public string Why { get; set; } = string.Empty;

        public int StepNumber
        {
            get => _stepNumber;
            set
            {
                if (_stepNumber != value)
                {
                    _stepNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

