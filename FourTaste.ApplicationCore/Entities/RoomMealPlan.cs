using System;

namespace FourTaste.ApplicationCore.Entities
{
    public class RoomMealPlan
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int UserId { get; set; } 
        public string? RecipeName { get; set; } = string.Empty;
        public int? RecipeId { get; set; }
        public DateTime PlannedDate { get; set; }
        public bool IsCooked { get; set; } = false;
    }
}