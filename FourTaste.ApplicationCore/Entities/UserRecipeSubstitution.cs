namespace FourTaste.ApplicationCore.Entities
{
    public class UserRecipeSubstitution
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RecipeId { get; set; }

        public int OriginalIngredientId { get; set; }

        public string OriginalIngredientName { get; set; } = string.Empty;

        public string SubstituteIngredientName { get; set; } = string.Empty;

        public string CalculatedAmount { get; set; } = string.Empty;

        public double Ratio { get; set; } = 1.0;

        public string? Note { get; set; }
    }
}
