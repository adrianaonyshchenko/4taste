using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Entities
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;       
        public string Category { get; set; } = string.Empty;    
        public string Cuisine { get; set; } = string.Empty;     
        public bool IsPublic { get; set; }                      
        public int UserId { get; set; }
        public byte[]? Photo { get; set; }
        public List<Ingredient> Ingredients { get; set; } = new ();
        public List<RecipeStep> Steps { get; set; } = new ();
    }
}