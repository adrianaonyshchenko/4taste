using System;

namespace FourTaste.ApplicationCore.Entities
{
    public class RoomMember
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public RoomRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
