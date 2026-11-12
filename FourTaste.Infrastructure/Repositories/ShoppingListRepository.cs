using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace FourTaste.Infrastructure.Repositories
{
    public class ShoppingListRepository : IShoppingListRepository
    {
        private readonly AppDbContext _context;

        public ShoppingListRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShoppingListItem>> GetByRoomIdAsync(int roomId)
        {
            return await _context.ShoppingListItems
                .Where(item => item.RoomId == roomId)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ShoppingListItem> items)
        {
            await _context.ShoppingListItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public async Task ClearRoomListAsync(int roomId)
        {
            var items = await _context.ShoppingListItems
                .Where(item => item.RoomId == roomId)
                .ToListAsync();

            if (items.Count > 0)
            {
                _context.ShoppingListItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }
    }
}