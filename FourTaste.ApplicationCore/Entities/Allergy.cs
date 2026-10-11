namespace FourTaste.ApplicationCore.Entities
{
    public class Allergy
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string AllergenName { get; set; } = string.Empty;
    }
}
