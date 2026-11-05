namespace FourTaste.ApplicationCore.Entities
{
    public class IngredientSubstitute
    {
        public int Id { get; set; }

        public string OriginalIngredientName { get; set; } = string.Empty;

        public string SubstituteName { get; set; } = string.Empty;

        public double Ratio { get; set; } = 1.0;

        public string? Note { get; set; }

        public string AllergenCodes { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = true;
    }
}
