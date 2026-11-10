using System;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public RoomRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> InviteCodeExistsAsync(string inviteCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Rooms
                .AsNoTracking()
                .AnyAsync(r => r.InviteCode == inviteCode);
        }

        public async Task<Room> CreateRoomAsync(Room room)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Rooms.Add(room);
            await context.SaveChangesAsync();
            return room;
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Rooms
                .Include(r => r.Members)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Room?> GetByInviteCodeAsync(string inviteCode)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Rooms
                .Include(r => r.Members)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.InviteCode == inviteCode);
        }

        public async Task<List<Room>> GetUserRoomsAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Rooms
                .AsNoTracking()
                .Where(r => r.OwnerId == userId || r.Members.Any(m => m.UserId == userId))
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> AddMemberAsync(RoomMember member)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.RoomMembers.Add(member);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int roomId, int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var member = await context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);
            if (member is null)
            {
                return false;
            }

            context.RoomMembers.Remove(member);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var ids = userIds.Distinct().ToList();
            return await context.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.ID))
                .ToDictionaryAsync(u => u.ID, u => string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name);
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (room is null)
            {
                return false;
            }

            context.Rooms.Remove(room);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
