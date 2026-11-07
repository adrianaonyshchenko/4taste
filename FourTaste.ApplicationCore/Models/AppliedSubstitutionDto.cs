namespace FourTaste.ApplicationCore.Models
{
    public class AppliedSubstitutionDto
    {
        public int Id { get; set; }

        public int RecipeId { get; set; }

        public int OriginalIngredientId { get; set; }

        public string OriginalIngredientName { get; set; } = string.Empty;

        public string SubstituteIngredientName { get; set; } = string.Empty;

        public string CalculatedAmount { get; set; } = string.Empty;

        public double Ratio { get; set; } = 1.0;

        public string? Note { get; set; }
    }
}

