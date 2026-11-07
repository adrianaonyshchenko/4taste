using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace FourTaste.ApplicationCore.Entities
{
    public class RecipeReview
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ReviewTag> Tags { get; set; } = new ();

        [NotMapped]
        public string AuthorName { get; set; } = string.Empty;
        public byte[]? Photo { get; set; }
    }
}