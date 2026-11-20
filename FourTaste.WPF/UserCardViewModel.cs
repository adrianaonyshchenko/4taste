using System;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Models;

namespace FourTaste.WPF
{
    public sealed class UserCardViewModel
    {
        private const int AvatarDecodeWidth = 96;

        public UserCardViewModel(UserSummaryDto user)
        {
            this.User = user;
            this.Avatar = ImageDecoder.Decode(user.Avatar, AvatarDecodeWidth);
        }

        public UserSummaryDto User { get; }

        public BitmapImage? Avatar { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(this.User.Name) ? this.User.Username : this.User.Name;

        public string UsernameText => $"@{this.User.Username}";

        public string Email => this.User.Email;

        public string Initial => this.DisplayName.Length > 0 ? this.DisplayName.Substring(0, 1).ToUpperInvariant() : "?";

        public string RecipesText => $"{this.User.RecipesCount} {RecipeWord(this.User.RecipesCount)}";

        public string RegisteredText => this.User.CreatedAt is DateTime createdAt
            ? $"з {createdAt.ToLocalTime():dd.MM.yyyy}"
            : "дата реєстрації —";

        public bool IsAdmin => this.User.IsAdmin;

        public static string RecipeWord(int count)
        {
            int mod10 = count % 10;
            int mod100 = count % 100;
            return (mod10, mod100) switch
            {
                (1, not 11) => "рецепт",
                (>= 2 and <= 4, < 10 or >= 20) => "рецепти",
                _ => "рецептів",
            };
        }
    }
}
