using System.Collections.Generic;
using FourTaste.ApplicationCore.Entities;

namespace FourTaste.ApplicationCore.Services
{
    public static class SubstituteSeedData
    {
        public static List<AllergenCatalogItem> GetAllergens()
        {
            return new List<AllergenCatalogItem>
            {
                new AllergenCatalogItem
                {
                    Id = 1,
                    Code = "milk",
                    NameUk = "Молоко",
                    Keywords = "молоко, лактоза, вершки, масло, сир, кефір, сметана, сироватка, йогурт, казеїн, творог, молочний",
                },
                new AllergenCatalogItem
                {
                    Id = 2,
                    Code = "gluten",
                    NameUk = "Глютен",
                    Keywords = "глютен, пшениця, пшеничне, борошно, жито, ячмінь, манна, кускус, булгур, овес",
                },
                new AllergenCatalogItem
                {
                    Id = 3,
                    Code = "egg",
                    NameUk = "Яйця",
                    Keywords = "яйце, яйця, яєчний, жовток, білок, меланж",
                },
                new AllergenCatalogItem
                {
                    Id = 4,
                    Code = "peanut",
                    NameUk = "Арахіс",
                    Keywords = "арахіс, арахісова, арахісове",
                },
                new AllergenCatalogItem
                {
                    Id = 5,
                    Code = "nuts",
                    NameUk = "Горіхи",
                    Keywords = "горіх, горіхи, мигдаль, мигдальне, фундук, кеш'ю, волоський, пекан, фісташки, горіховий",
                },
                new AllergenCatalogItem
                {
                    Id = 6,
                    Code = "soy",
                    NameUk = "Соя",
                    Keywords = "соя, соєвий, соєве, соєве молоко, тофу",
                },
                new AllergenCatalogItem
                {
                    Id = 7,
                    Code = "fish",
                    NameUk = "Риба",
                    Keywords = "риба, рибний, рибне, лосось, тунець, тріска",
                },
                new AllergenCatalogItem
                {
                    Id = 8,
                    Code = "honey",
                    NameUk = "Мед",
                    Keywords = "мед, медовий, медове",
                },
            };
        }

        public static List<IngredientSubstitute> GetSubstitutes()
        {
            return new List<IngredientSubstitute>
            {
                // Замінники для молока
                new IngredientSubstitute
                {
                    Id = 1,
                    OriginalIngredientName = "молоко",
                    SubstituteName = "Вівсяне молоко",
                    Ratio = 1.0,
                    Note = "Добре підходить для випічки та десертів",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 2,
                    OriginalIngredientName = "молоко",
                    SubstituteName = "Кокосове молоко",
                    Ratio = 1.0,
                    Note = "Має ніжний кремовий смак",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 3,
                    OriginalIngredientName = "молоко",
                    SubstituteName = "Мигдальне молоко",
                    Ratio = 1.0,
                    Note = "Легкий горіховий присмак",
                    AllergenCodes = "nuts",
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 4,
                    OriginalIngredientName = "молоко",
                    SubstituteName = "Соєве молоко",
                    Ratio = 1.0,
                    Note = "Універсальний рослинний замінник",
                    AllergenCodes = "soy",
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 5,
                    OriginalIngredientName = "молоко",
                    SubstituteName = "Рисове молоко",
                    Ratio = 1.0,
                    Note = "Гіпоалергенне, легке і солодкувате",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },

                // Замінники для вершкового масла
                new IngredientSubstitute
                {
                    Id = 6,
                    OriginalIngredientName = "масло",
                    SubstituteName = "Кокосова олія",
                    Ratio = 1.0,
                    Note = "Ідеально для випічки та смаження",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 7,
                    OriginalIngredientName = "масло",
                    SubstituteName = "Оливкова олія",
                    Ratio = 0.8,
                    Note = "Для гарячих страв та соусів",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 8,
                    OriginalIngredientName = "масло",
                    SubstituteName = "Рослинний спред без лактози",
                    Ratio = 1.0,
                    Note = "Для бутербродів та тіста",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },

                // Замінники для борошна (глютен)
                new IngredientSubstitute
                {
                    Id = 9,
                    OriginalIngredientName = "борошно",
                    SubstituteName = "Рисове борошно",
                    Ratio = 1.0,
                    Note = "Безглютеновий універсальний замінник",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 10,
                    OriginalIngredientName = "борошно",
                    SubstituteName = "Кукурудзяне борошно",
                    Ratio = 0.9,
                    Note = "Надає ніжний золотистий колір",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 11,
                    OriginalIngredientName = "борошно",
                    SubstituteName = "Мигдальне борошно",
                    Ratio = 1.0,
                    Note = "Для вишуканої випічки та печива",
                    AllergenCodes = "nuts",
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 12,
                    OriginalIngredientName = "борошно",
                    SubstituteName = "Гречане борошно",
                    Ratio = 1.0,
                    Note = "Для млинців та хліба",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },

                // Замінники для яєць
                new IngredientSubstitute
                {
                    Id = 13,
                    OriginalIngredientName = "яйця",
                    SubstituteName = "Яблучне пюре (60 г)",
                    Ratio = 1.0,
                    Note = "Для солодкої випічки та бісквітів",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 14,
                    OriginalIngredientName = "яйця",
                    SubstituteName = "Бананове пюре (1/2 шт)",
                    Ratio = 1.0,
                    Note = "Надає банановий аромат та зв'язність",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 15,
                    OriginalIngredientName = "яйця",
                    SubstituteName = "Насіння льону з водою",
                    Ratio = 1.0,
                    Note = "1 ст. л. змеленого льону + 3 ст. л. теплої води",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 16,
                    OriginalIngredientName = "яйця",
                    SubstituteName = "Аквафаба (3 ст. л.)",
                    Ratio = 1.0,
                    Note = "Збивається у стійку піну як білок",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },

                // Замінники для арахісу
                new IngredientSubstitute
                {
                    Id = 17,
                    OriginalIngredientName = "арахіс",
                    SubstituteName = "Насіння соняшника",
                    Ratio = 1.0,
                    Note = "Безпечна хрустка альтернатива",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 18,
                    OriginalIngredientName = "арахіс",
                    SubstituteName = "Насіння гарбуза",
                    Ratio = 1.0,
                    Note = "Багате на корисні жири та мікроелементи",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 19,
                    OriginalIngredientName = "арахіс",
                    SubstituteName = "Волоські горіхи",
                    Ratio = 1.0,
                    Note = "Підходить для тих, у кого немає алергії на горіхи",
                    AllergenCodes = "nuts",
                    IsVerified = true,
                },

                // Замінники для меду
                new IngredientSubstitute
                {
                    Id = 20,
                    OriginalIngredientName = "мед",
                    SubstituteName = "Кленовий сироп",
                    Ratio = 1.0,
                    Note = "Природний рідкий підсолоджувач",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 21,
                    OriginalIngredientName = "мед",
                    SubstituteName = "Сироп агави",
                    Ratio = 0.8,
                    Note = "М'яка солодкість без продуктів бджільництва",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
                new IngredientSubstitute
                {
                    Id = 22,
                    OriginalIngredientName = "мед",
                    SubstituteName = "Яблучний сироп",
                    Ratio = 1.0,
                    Note = "Фруктовий натуральний сироп",
                    AllergenCodes = string.Empty,
                    IsVerified = true,
                },
            };
        }
    }
}
