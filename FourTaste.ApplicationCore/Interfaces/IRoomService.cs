using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRoomService
    {
        Task<Result<RoomDto>> CreateRoomAsync(CreateRoomRequest request);
        Task<Result<List<RoomDto>>> GetUserRoomsAsync(int userId);
        Task<Result<RoomDetailsDto>> GetRoomDetailsAsync(int roomId, int userId);
        Task<Result> DeleteRoomAsync(int roomId, int userId);
        Task<Result> LeaveRoomAsync(int roomId, int userId);
        Task<Result<RoomDto>> JoinRoomByCodeAsync(string inviteCode, int userId);
    }
}
