namespace FourTaste.ApplicationCore.Models
{
    public class AddShoppingItemRequest
    {
        public int UserId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; } = "шт";
    }
}