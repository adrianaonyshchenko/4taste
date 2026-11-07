using System.Collections.Generic;

namespace FourTaste.ApplicationCore.Models
{
    public static class ReviewOptions
    {
        public const int MinRating = 1;
        public const int MaxRating = 5;
        public const int MaxTextLength = 1000;
        public const int MaxTags = 5;
        public const int MaxPhotoSizeBytes = 1024 * 1024;
        public static IReadOnlyList<string> Tags { get; } = new[]
        {
            "занадто солоне",
            "занадто солодке",
            "занадто гостре",
            "прісне",
            "жирне",
            "кисле",
            "ароматне",
            "ніжне",
            "ситне",
            "легке",
        };
    }
}