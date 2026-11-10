using System;
using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerId { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<RoomMember> Members { get; set; } = new ();
    }
}
