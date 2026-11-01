using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IFridgeRepository
    {
        Task AddAsync(UserFridgeItem item);
        Task RemoveAsync(int itemId);
        Task<List<UserFridgeItem>> GetByUserIdAsync(int userId);
        Task<bool> ExistsAsync(int userId, string ingredientName);
        Task<UserFridgeItem?> GetByIdAsync(int itemId);

        Task AddShoppingItemAsync(UserShoppingItem item);
        Task RemoveShoppingItemAsync(int itemId);
        Task<List<UserShoppingItem>> GetShoppingItemsByUserIdAsync(int userId);
        Task<UserShoppingItem?> GetShoppingItemByIdAsync(int itemId);
    }
}
