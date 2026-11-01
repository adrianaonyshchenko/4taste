using System.Collections.Generic;

namespace FourTaste.WPF
{
    public static class RecipeOptions
    {
        public static readonly IReadOnlyList<string> Categories = new List<string>
        {
            "Сніданок", "Обід", "Вечеря", "Супи та бульйони", "Основні страви",
            "Салати", "Закуски", "Десерти та випічка", "Напої та коктейлі", "Інше",
        };

        public static readonly IReadOnlyList<string> Cuisines = new List<string>
        {
            "Українська", "Польська", "Італійська", "Японська", "Мексиканська",
            "Французька", "Американська", "Грузинська", "Китайська", "Інша",
        };
    }
}