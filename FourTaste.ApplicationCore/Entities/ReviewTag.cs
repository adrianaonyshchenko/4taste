namespace FourTaste.ApplicationCore.Entities
{
    public class ReviewTag
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}