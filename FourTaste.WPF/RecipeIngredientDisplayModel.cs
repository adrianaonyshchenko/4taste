using System.Windows;
using System.Windows.Media;

namespace FourTaste.WPF
{
    public class RecipeIngredientDisplayModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Amount { get; set; } = string.Empty;

        public bool IsAllergen { get; set; }

        public bool IsSubstituted { get; set; }

        public string OriginalName { get; set; } = string.Empty;

        public string OriginalAmount { get; set; } = string.Empty;

        public string? SubstituteNote { get; set; }

        public Brush TextColor
        {
            get
            {
                if (this.IsSubstituted)
                {
                    return new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }

                if (this.IsAllergen)
                {
                    return new SolidColorBrush(Color.FromRgb(217, 56, 41));
                }

                return new SolidColorBrush(Color.FromRgb(74, 53, 47));
            }
        }

        public Visibility AllergenWarningVisibility => (this.IsAllergen && !this.IsSubstituted) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility SubstitutedBadgeVisibility => this.IsSubstituted ? Visibility.Visible : Visibility.Collapsed;

        public Visibility RevertButtonVisibility => this.IsSubstituted ? Visibility.Visible : Visibility.Collapsed;
    }
}
