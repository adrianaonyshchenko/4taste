using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public class SafeSubstituteItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public double Ratio { get; set; } = 1.0;

        public string? Note { get; set; }

        public IReadOnlyList<string> AllergenCodes { get; set; } = new List<string>();

        public string CalculatedAmount { get; set; } = string.Empty;
    }
}

