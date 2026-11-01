using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FourTaste.Tests
{
    public class RecipeProductSearchTests
    {
        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;
            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
            public AppDbContext CreateDbContext() => new AppDbContext(_options);
        }

        private static Recipe MakeRecipe(int id, string title, string category, string cuisine, string time, bool isPublic, int userId, params string[] ingredients)
        {
            return new Recipe
            {
                Id = id,
                Title = title,
                Category = category,
                Cuisine = cuisine,
                Time = time,
                IsPublic = isPublic,
                UserId = userId,
                Ingredients = ingredients.Select(n => new Ingredient { Name = n }).ToList(),
            };
        }

        private static RecipeRepository CreateRepository(params Recipe[] extraRecipes)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var factory = new TestDbContextFactory(options);

            using (var context = factory.CreateDbContext())
            {
                context.Users.AddRange(
                    new User { ID = 10, Name = "Оксана", Username = "oksana" },
                    new User { ID = 20, Name = "Іван", Username = "ivan" });

                context.Recipes.AddRange(
                    MakeRecipe(1, "Млинці", "Сніданок", "Українська", "30:00", true, 10, "Борошно пшеничне 200 г", "Молоко 500 мл", "Яйця 2 шт"),
                    MakeRecipe(2, "Омлет", "Сніданок", "Французька", "10:00", true, 20, "Яйця 3 шт", "Молоко 100 мл"),
                    MakeRecipe(3, "Борщ", "Обід", "Українська", "90:00", true, 20, "Буряк", "Капуста", "Картопля", "М'ясо"),
                    MakeRecipe(4, "Арахісове печиво", "Десерти", "Американська", "45:00", true, 10, "Борошно", "Арахіс 100 г", "Цукор"),
                    MakeRecipe(5, "Приватний пиріг", "Десерти", "Українська", "60:00", false, 20, "Борошно", "Яйця"),
                    MakeRecipe(6, "Мій приватний салат", "Салати", "Українська", "15:00", false, 10, "Огірок"));
                context.Recipes.AddRange(extraRecipes);
                context.SaveChanges();
            }

            return new RecipeRepository(factory);
        }

        [Fact]
        public async Task Search_ByProduct_MatchesIngredientTextCaseInsensitive()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "БОРОШНО" },
            });

            Assert.Equal(new[] { 1, 4 }, result.Select(r => r.Id).OrderBy(i => i).ToArray());
        }

        [Fact]
        public async Task Search_ByProducts_BestMatchFirst()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця", "молоко" },
            });

            Assert.Equal(new[] { 2, 1 }, result.Select(r => r.Id).ToArray());
        }

        [Fact]
        public async Task Search_ByProducts_FillsMatchedAndTotalCounts()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця", "молоко" },
            });

            var pancakes = result.Single(r => r.Id == 1);
            Assert.Equal(2, pancakes.MatchedIngredientsCount);
            Assert.Equal(3, pancakes.TotalIngredientsCount);
        }

        [Fact]
        public async Task Search_ExcludedAllergen_RemovesRecipesWithIt()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "борошно" },
                ExcludedAllergens = new List<string> { "Арахіс" },
            });

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task Search_ByProductsAndCategory_FiltersCaseInsensitive()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "молоко" },
                Category = "сніданок",
            });

            Assert.Equal(new[] { 1, 2 }, result.Select(r => r.Id).OrderBy(i => i).ToArray());
        }

        [Fact]
        public async Task Search_ByProductsAndCuisine_Filters()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "молоко" },
                Cuisine = "Французька",
            });

            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public async Task Search_MaxCookingMinutes_ExcludesLongerRecipes()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 20,
            });

            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public async Task Search_MaxCookingMinutes_ComparesSecondsToo()
        {
            var repo = CreateRepository(MakeRecipe(7, "Яєчня", "Сніданок", "Українська", "20:30", true, 10, "Яйця"));

            var within20 = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 20,
            });
            var within21 = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 21,
            });

            Assert.Equal(new[] { 2 }, within20.Select(r => r.Id).OrderBy(i => i).ToArray());
            Assert.Equal(new[] { 2, 7 }, within21.Select(r => r.Id).OrderBy(i => i).ToArray());
        }

        [Fact]
        public async Task Search_MaxCookingMinutes_HandlesRecipesOverHundredMinutes()
        {
            var repo = CreateRepository(MakeRecipe(8, "Довга запіканка", "Обід", "Українська", "120:00", true, 10, "Яйця"));

            var within90 = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 90,
            });
            var within150 = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 150,
            });

            Assert.DoesNotContain(within90, r => r.Id == 8);
            Assert.Contains(within150, r => r.Id == 8);
        }

        [Fact]
        public async Task Search_MaxCookingMinutes_ExcludesRecipesWithUnparsableTime()
        {
            var repo = CreateRepository(MakeRecipe(9, "Швидка яєчня", "Сніданок", "Українська", "швидко", true, 10, "Яйця"));

            var withLimit = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
                MaxCookingMinutes = 60,
            });
            var withoutLimit = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "яйця" },
            });

            Assert.DoesNotContain(withLimit, r => r.Id == 9);
            Assert.Contains(withoutLimit, r => r.Id == 9);
        }

        [Fact]
        public async Task Search_Viewer_SeesPublicAndOwnPrivateButNotOthersPrivate()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                ViewerUserId = 10,
                AvailableProducts = new List<string> { "яйця", "огірок" },
            });

            var ids = result.Select(r => r.Id).ToList();
            Assert.Contains(6, ids);
            Assert.Contains(2, ids);
            Assert.DoesNotContain(5, ids);
        }

        [Fact]
        public async Task Search_NoProducts_DoesNotApplyProductFilter()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string>(),
            });

            Assert.Equal(4, result.Count);
            Assert.All(result, r => Assert.Equal(0, r.MatchedIngredientsCount));
        }

        [Fact]
        public async Task Search_BlankProducts_AreIgnored()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(new RecipeSearchCriteria
            {
                OnlyPublic = true,
                AvailableProducts = new List<string> { "  ", string.Empty },
            });

            Assert.Equal(4, result.Count);
        }
    }
}