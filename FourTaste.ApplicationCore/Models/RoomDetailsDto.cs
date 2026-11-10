using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class RoomDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerId { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public List<RoomMemberDto> Members { get; set; } = new ();
    }
}
