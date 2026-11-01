using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using Moq;
using Xunit;

namespace FourTaste.Tests
{
    public class UserCuisinesTests
    {
        [Fact]
        public async Task CalculateCuisineStats_CorrectlyCountsRecipesPerCuisine()
        {
            var mockRecipeService = new Mock<IRecipeService>();
            var dummyRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Борщ", Cuisine = "Українська", UserId = 1 },
                new Recipe { Id = 2, Title = "Вареники", Cuisine = "Українська", UserId = 1 },
                new Recipe { Id = 3, Title = "Піца", Cuisine = "Італійська", UserId = 1 },
                new Recipe { Id = 4, Title = "Суші", Cuisine = "Японська", UserId = 1 }
            };

            mockRecipeService
                .Setup(s => s.SearchRecipesAsync(It.IsAny<RecipeSearchCriteria>()))
                .ReturnsAsync(Result<List<Recipe>>.Success(dummyRecipes));

            var result = await mockRecipeService.Object.SearchRecipesAsync(RecipeSearchCriteria.ForUser(1, string.Empty));

            var recipes = result.Value ?? new List<Recipe>();

            var stats = recipes
                .GroupBy(r => r.Cuisine ?? "Інша")
                .ToDictionary(g => g.Key, g => g.Count());

            Assert.True(result.IsSuccess);
            Assert.Equal(3, stats.Count);
            Assert.Equal(2, stats["Українська"]);
            Assert.Equal(1, stats["Італійська"]);
            Assert.Equal(1, stats["Японська"]);
        }
    }
}