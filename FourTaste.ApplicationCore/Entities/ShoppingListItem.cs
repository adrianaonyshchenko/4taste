namespace FourTaste.ApplicationCore.Entities
{
    public class ShoppingListItem
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = false;
    }
}
