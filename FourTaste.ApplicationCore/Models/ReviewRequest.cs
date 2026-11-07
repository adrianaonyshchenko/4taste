using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class ReviewRequest
    {
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
        public List<string> Tags { get; set; } = new ();
        public byte[]? Photo { get; set; }
    }
}