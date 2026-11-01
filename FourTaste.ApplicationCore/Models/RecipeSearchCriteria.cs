using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Models
{
    public class RecipeSearchCriteria
    {
        public string? SearchTerm { get; set; }

        public RecipeSearchFilter Filter { get; set; } = RecipeSearchFilter.All;

        public int? OwnerUserId { get; set; }

        public bool OnlyPublic { get; set; }

        public static RecipeSearchCriteria Public(string? searchTerm = null, RecipeSearchFilter filter = RecipeSearchFilter.All)
            => new () { SearchTerm = searchTerm, Filter = filter, OnlyPublic = true };

        public static RecipeSearchCriteria ForUser(int userId, string? searchTerm = null)
            => new () { SearchTerm = searchTerm, OwnerUserId = userId };
    }
}