using System.Collections.Generic;
using System.Linq;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Models
{
    public class ReviewsOverview
    {
        public bool IsRecipeOwner { get; init; }
        public List<RecipeReview> Reviews { get; init; } = new ();

        public IReadOnlyList<KeyValuePair<string, int>> TagCounts =>
            Reviews
                .SelectMany(r => r.Tags)
                .GroupBy(t => t.Name)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key)
                .ToList();
    }
}