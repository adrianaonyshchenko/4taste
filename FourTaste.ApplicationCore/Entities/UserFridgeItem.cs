namespace FourTaste.ApplicationCore.Entities
{
    public class UserFridgeItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Unit { get; set; } = "шт";
}
}
 