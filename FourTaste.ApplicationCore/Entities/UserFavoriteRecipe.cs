namespace FourTaste.ApplicationCore.Entities
{
    public class UserFavoriteRecipe
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RecipeId { get; set; }
    }
}