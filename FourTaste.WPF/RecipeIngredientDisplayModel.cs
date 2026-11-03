using System.Windows.Media;

namespace FourTaste.WPF
{
    public class RecipeIngredientDisplayModel
    {
        public string Name { get; set; } = string.Empty;

        public string Amount { get; set; } = string.Empty;

        public bool IsAllergen { get; set; }

        public Brush TextColor => IsAllergen
            ? new SolidColorBrush(Color.FromRgb(217, 56, 41))
            : new SolidColorBrush(Color.FromRgb(74, 53, 47));
    }
}

