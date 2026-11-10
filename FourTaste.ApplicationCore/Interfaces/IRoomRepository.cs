using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRoomRepository
    {
        Task<bool> InviteCodeExistsAsync(string inviteCode);
        Task<Room> CreateRoomAsync(Room room);
        Task<Room?> GetByIdAsync(int id);
        Task<Room?> GetByInviteCodeAsync(string inviteCode);
        Task<List<Room>> GetUserRoomsAsync(int userId);
        Task<bool> AddMemberAsync(RoomMember member);
        Task<bool> RemoveMemberAsync(int roomId, int userId);
        Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);
        Task<bool> DeleteRoomAsync(int id);
    }
}
