namespace FourTaste.ApplicationCore.Models
{
    public class CreateRoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
