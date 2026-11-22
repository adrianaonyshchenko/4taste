using System;

namespace FourTaste.ApplicationCore.Entities
{
    public class RecipeCategory
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}