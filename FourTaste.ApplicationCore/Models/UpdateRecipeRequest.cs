using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class UpdateRecipeRequest
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Time { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Cuisine { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        public int Servings { get; set; } = 1;

        public byte[]? Photo { get; set; }

        public bool UpdatePhoto { get; set; } = true;

        public List<RecipeIngredientDto> Ingredients { get; set; } = new ();

        public List<string> Steps { get; set; } = new ();
    }
}

