namespace FourTaste.ApplicationCore.Entities
{
    public class RecipeStep
    {
        public int Id { get; set; }
        public int StepNumber { get; set; }       
        public string Description { get; set; } = string.Empty; 
        public int RecipeId { get; set; }
    }
}
