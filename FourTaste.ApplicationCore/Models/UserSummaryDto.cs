using System;

namespace FourTaste.ApplicationCore.Models
{
    public class UserSummaryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public byte[]? Avatar { get; set; }

        public int RecipesCount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public bool IsAdmin { get; set; }
    }
}
