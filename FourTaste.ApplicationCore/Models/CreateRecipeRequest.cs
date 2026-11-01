using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class CreateRecipeRequest
    {
        public string Title { get; set; } = string.Empty;

        public string Time { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Cuisine { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        public int UserId { get; set; }

        public List<string> Ingredients { get; set; } = new ();

        public List<string> Steps { get; set; } = new ();

        public byte[]? Photo { get; set; }
    }
}