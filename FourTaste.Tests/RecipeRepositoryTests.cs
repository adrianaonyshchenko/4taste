using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FourTaste.Tests
{
    public class RecipeRepositoryTests
    {
        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

            public AppDbContext CreateDbContext() => new AppDbContext(_options);
        }

        private static RecipeRepository CreateRepository()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var factory = new TestDbContextFactory(options);

            using (var context = factory.CreateDbContext())
            {
                context.Users.AddRange(
                    new User { ID = 10, Name = "Оксана Шевченко", Username = "oksana_chef" },
                    new User { ID = 20, Name = "Іван Франко", Username = "ivan_cook" });

                context.Recipes.AddRange(
                    new Recipe { Id = 1, Title = "Київський торт", Category = "Десерти та випічка", Cuisine = "Українська", IsPublic = true, UserId = 10 },
                    new Recipe { Id = 2, Title = "Вареники з вишнею", Category = "Десерти та випічка", Cuisine = "Українська", IsPublic = true, UserId = 20 },
                    new Recipe { Id = 3, Title = "Паста", Category = "Основні страви", Cuisine = "Італійська", IsPublic = false, UserId = 10 },
                    new Recipe { Id = 4, Title = "Суп з рибою", Category = "Супи", Cuisine = "Інша", IsPublic = true, UserId = 999 });
                context.SaveChanges();
            }

            return new RecipeRepository(factory);
        }

        [Fact]
        public async Task Search_Public_ReturnsOnlyPublicRecipes()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public());

            Assert.Equal(3, result.Count);
            Assert.DoesNotContain(result, r => !r.IsPublic);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Search_EmptyTerm_ReturnsAllMatchingScope(string? term)
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.ForUser(10, term));

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Search_ByTitle_CaseInsensitiveAndPartial()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("київ"));

            Assert.Single(result);
            Assert.Equal("Київський торт", result[0].Title);
        }

        [Fact]
        public async Task Search_ByAuthorName_FillsAuthorFields()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("Оксана"));

            Assert.Single(result);
            Assert.Equal("Оксана Шевченко", result[0].AuthorName);
            Assert.Equal("oksana_chef", result[0].AuthorUsername);
        }

        [Fact]
        public async Task Search_ByUsernameWithAtSign_MatchesAuthor()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("@oksana_chef"));

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task Search_TitleOnly_DoesNotMatchAuthor()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("Оксана", RecipeSearchFilter.TitleOnly));

            Assert.Empty(result);
        }

        [Fact]
        public async Task Search_AuthorOnly_DoesNotMatchTitle()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("торт", RecipeSearchFilter.AuthorOnly));

            Assert.Empty(result);
        }

        [Fact]
        public async Task Search_ByCuisine_MatchesInOwnRecipes()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.ForUser(10, "Італійська"));

            Assert.Single(result);
            Assert.Equal("Паста", result[0].Title);
        }

        [Fact]
        public async Task Search_NoMatch_ReturnsEmptyList()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("піца"));

            Assert.Empty(result);
        }

        [Fact]
        public async Task Search_AuthorMissing_UsesFallbackName()
        {
            var repo = CreateRepository();

            var result = await repo.SearchAsync(RecipeSearchCriteria.Public("Суп з"));

            Assert.Single(result);
            Assert.Equal("Невідомий автор", result[0].AuthorName);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesRecipeFieldsIngredientsAndSteps()
        {
            var repo = CreateRepository();
            var updated = new Recipe
            {
                Id = 1,
                Title = "Оновлений Київський Торт",
                Time = "02:30",
                Category = "Десерти та випічка",
                Cuisine = "Українська",
                IsPublic = false,
                Servings = 8,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Name = "Горіхи", Amount = "200г." },
                    new Ingredient { Name = "Цукор", Amount = "150г." },
                },
                Steps = new List<RecipeStep>
                {
                    new RecipeStep { StepNumber = 1, Description = "Збити білки" },
                    new RecipeStep { StepNumber = 2, Description = "Випекти коржі" },
                },
            };

            await repo.UpdateAsync(updated);

            var loaded = await repo.GetByIdAsync(1);
            Assert.NotNull(loaded);
            Assert.Equal("Оновлений Київський Торт", loaded!.Title);
            Assert.Equal("02:30", loaded.Time);
            Assert.False(loaded.IsPublic);
            Assert.Equal(8, loaded.Servings);
            Assert.Equal(2, loaded.Ingredients.Count);
            Assert.Equal(2, loaded.Steps.Count);
        }

        [Fact]
        public async Task DeleteAsync_RemovesRecipeFromDatabase()
        {
            var repo = CreateRepository();

            await repo.DeleteAsync(2);

            var loaded = await repo.GetByIdAsync(2);
            Assert.Null(loaded);
        }
    }
}
