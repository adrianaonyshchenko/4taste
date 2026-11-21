using System;

namespace FourTaste.ApplicationCore.Entities
{
    public class UserWarning
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? AdminId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}