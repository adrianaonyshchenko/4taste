using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRoomService
    {
        Task<Result<RoomDto>> CreateRoomAsync(CreateRoomRequest request);
        Task<Result<List<RoomDto>>> GetUserRoomsAsync();
        Task<Result<RoomDetailsDto>> GetRoomDetailsAsync(int roomId);
        Task<Result> DeleteRoomAsync(int roomId);
        Task<Result> LeaveRoomAsync(int roomId);
        Task<Result<RoomDto>> JoinRoomByCodeAsync(string inviteCode);
    }
}
