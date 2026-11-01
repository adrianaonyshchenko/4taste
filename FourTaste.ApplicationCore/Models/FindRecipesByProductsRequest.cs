namespace FourTaste.ApplicationCore.Models
{
    public class FindRecipesByProductsRequest
    {
        public int UserId { get; set; }
        public string? Category { get; set; }
        public string? Cuisine { get; set; }
        public int? MaxCookingMinutes { get; set; }
    }
}