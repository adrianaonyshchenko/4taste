using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class SafeSubstitutesResponse
    {
        public int RecipeId { get; set; }

        public int IngredientId { get; set; }

        public string OriginalIngredientName { get; set; } = string.Empty;

        public string OriginalAmount { get; set; } = string.Empty;

        public IReadOnlyList<string> MatchedUserAllergens { get; set; } = new List<string>();

        public IReadOnlyList<SafeSubstituteItemDto> Substitutes { get; set; } = new List<SafeSubstituteItemDto>();
    }
}

