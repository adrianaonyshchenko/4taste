using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IShoppingListRepository
    {
        Task<List<ShoppingListItem>> GetByRoomIdAsync(int roomId);
        Task AddRangeAsync(IEnumerable<ShoppingListItem> items);
        Task AddAsync(ShoppingListItem item);
        Task DeleteAsync(int roomId, int itemId);
        Task ClearRoomListAsync(int roomId);
    }
}
