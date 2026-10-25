using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        [NotMapped]
        public string AuthorName { get; set; } = string.Empty;

        [NotMapped]
        public string AuthorUsername { get; set; } = string.Empty;

        [NotMapped]
        public string AuthorDisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(AuthorName) && !string.IsNullOrWhiteSpace(AuthorUsername))
                {
                    return AuthorName.Equals(AuthorUsername, StringComparison.OrdinalIgnoreCase)
                        ? $"@{AuthorUsername}"
                        : $"{AuthorName} (@{AuthorUsername})";
                }

                if (!string.IsNullOrWhiteSpace(AuthorUsername))
                {
                    return $"@{AuthorUsername}";
                }

                if (!string.IsNullOrWhiteSpace(AuthorName))
                {
                    return AuthorName;
                }

                return "Невідомий автор";
            }
        }
    }
}
