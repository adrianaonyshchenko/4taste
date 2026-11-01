using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IFridgeService
    {
        Task<Result<int>> AddItemAsync(AddFridgeItemRequest request);
        Task<Result> RemoveItemAsync(RemoveFridgeItemRequest request);
        Task<Result<List<UserFridgeItem>>> GetUserFridgeAsync(int userId);

        Task<Result<int>> AddShoppingItemAsync(AddShoppingItemRequest request);
        Task<Result> RemoveShoppingItemAsync(RemoveShoppingItemRequest request);
        Task<Result<List<UserShoppingItem>>> GetShoppingListAsync(int userId);
    }
}