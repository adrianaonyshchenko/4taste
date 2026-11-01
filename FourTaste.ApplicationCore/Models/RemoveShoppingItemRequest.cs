namespace FourTaste.ApplicationCore.Models
{
    public class RemoveShoppingItemRequest
    {
        public int UserId { get; set; }
        public int ItemId { get; set; }
    }
}