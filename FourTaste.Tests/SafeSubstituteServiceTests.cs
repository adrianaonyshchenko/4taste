using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using FourTaste.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class SafeSubstituteServiceTests
    {
        private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;

            public TestDbContextFactory(DbContextOptions<AppDbContext> options)
            {
                _options = options;
            }

            public AppDbContext CreateDbContext() => new AppDbContext(_options);
        }

        private static (SubstituteService Service, AppDbContext Db, Mock<IRecipeRepository> RecipeRepoMock, Mock<IAllergyService> AllergyServiceMock) CreateTestSetup(
            params Allergy[] userAllergies)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var factory = new TestDbContextFactory(options);
            var db = factory.CreateDbContext();

            // Заповнюємо довідники за замовчуванням
            db.AllergenCatalogItems.AddRange(SubstituteSeedData.GetAllergens());
            db.IngredientSubstitutes.AddRange(SubstituteSeedData.GetSubstitutes());
            db.SaveChanges();

            var substituteRepo = new SubstituteRepository(factory);
            var recipeRepoMock = new Mock<IRecipeRepository>();
            var allergyServiceMock = new Mock<IAllergyService>();
            var loggerMock = new Mock<ILogger>();

            allergyServiceMock
                .Setup(a => a.GetAllergiesByUserIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int uid) =>
                {
                    var userList = userAllergies.Where(al => al.UserId == uid).ToList();
                    return Result<IReadOnlyList<Allergy>>.Success(userList);
                });

            var service = new SubstituteService(
                recipeRepoMock.Object,
                allergyServiceMock.Object,
                substituteRepo,
                loggerMock.Object);

            return (service, db, recipeRepoMock, allergyServiceMock);
        }

        private static Recipe CreateTestRecipe(int id = 1, int userId = 10, bool isPublic = true)
        {
            return new Recipe
            {
                Id = id,
                UserId = userId,
                Title = "Млинці",
                IsPublic = isPublic,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 101, RecipeId = id, Name = "Молоко коров'яче", Amount = "500 мл" },
                    new Ingredient { Id = 102, RecipeId = id, Name = "Борошно пшеничне", Amount = "200 г" },
                    new Ingredient { Id = 103, RecipeId = id, Name = "Яйця курячі", Amount = "2 шт." },
                    new Ingredient { Id = 104, RecipeId = id, Name = "Цукор", Amount = "2 ст. л." },
                },
            };
        }

        [Fact]
        public async Task GetSafeSubstitutes_ReturnsSubstitutesWithoutAllergens()
        {
            // Користувач з алергією лише на молоко
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            var result = await service.GetSafeSubstitutesAsync(42, 1, 101);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            var substitutes = result.Value.Substitutes;

            // Вівсяне, кокосове та рисове молоко повинні бути запропоновані
            Assert.Contains(substitutes, s => s.Name == "Вівсяне молоко");
            Assert.Contains(substitutes, s => s.Name == "Кокосове молоко");
            Assert.Contains(substitutes, s => s.Name == "Рисове молоко");
        }

        [Fact]
        public async Task GetSafeSubstitutes_FiltersOutSubstituteContainingAnotherUserAllergen()
        {
            // Користувач алергік і на молоко, і на горіхи
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" },
                new Allergy { Id = 2, UserId = 42, AllergenName = "горіхи" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            var result = await service.GetSafeSubstitutesAsync(42, 1, 101);

            Assert.True(result.IsSuccess);
            var substitutes = result.Value!.Substitutes;

            // Мигдальне молоко містить горіхи (AllergenCodes = "nuts") - його не має бути!
            Assert.DoesNotContain(substitutes, s => s.Name == "Мигдальне молоко");
            // Вівсяне молоко не містить горіхів - має бути запропоновано
            Assert.Contains(substitutes, s => s.Name == "Вівсяне молоко");
        }

        [Fact]
        public async Task GetSafeSubstitutes_FiltersOutSubstitutesAcrossMultipleAllergies()
        {
            // Користувач з алергією на молоко, сою та горіхи
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" },
                new Allergy { Id = 2, UserId = 42, AllergenName = "соя" },
                new Allergy { Id = 3, UserId = 42, AllergenName = "горіхи" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            var result = await service.GetSafeSubstitutesAsync(42, 1, 101);

            Assert.True(result.IsSuccess);
            var substitutes = result.Value!.Substitutes;

            // Мигдальне (горіхи) і соєве (соя) молоко відфільтровані
            Assert.DoesNotContain(substitutes, s => s.Name == "Мигдальне молоко");
            Assert.DoesNotContain(substitutes, s => s.Name == "Соєве молоко");
            // Вівсяне молоко залишається
            Assert.Contains(substitutes, s => s.Name == "Вівсяне молоко");
        }

        [Fact]
        public async Task GetSafeSubstitutes_CrossAllergenGroup_MatchesCorrectly()
        {
            // Користувач вказав алерген "лактоза" (перехресна категорія з milk)
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "лактоза" });

            var recipe = new Recipe
            {
                Id = 2,
                UserId = 10,
                IsPublic = true,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 201, RecipeId = 2, Name = "Вершкове масло", Amount = "100 г" },
                },
            };
            recipeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipe);

            var conflicting = await service.GetConflictingIngredientsAsync(42, 2);
            Assert.True(conflicting.IsSuccess);
            Assert.Single(conflicting.Value!);
            Assert.Equal("Вершкове масло", conflicting.Value![0].Name);

            var substitutesRes = await service.GetSafeSubstitutesAsync(42, 2, 201);
            Assert.True(substitutesRes.IsSuccess);
            Assert.Contains(substitutesRes.Value!.Substitutes, s => s.Name == "Кокосова олія");
        }

        [Fact]
        public async Task GetSafeSubstitutes_ReturnsEmptyListWhenNoSafeSubstitutesExist()
        {
            var (service, db, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "екзотична пряність" });

            var recipe = new Recipe
            {
                Id = 99,
                UserId = 10,
                IsPublic = true,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 991, RecipeId = 99, Name = "Екзотична пряність", Amount = "1 ч. л." },
                },
            };
            recipeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(recipe);

            var result = await service.GetSafeSubstitutesAsync(42, 99, 991);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!.Substitutes);
        }

        [Fact]
        public async Task GetSafeSubstitutes_CottageCheese_ReturnsTofuAndNotOils()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "кисломолочний сир" });

            var recipe = new Recipe
            {
                Id = 15,
                UserId = 10,
                IsPublic = true,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 151, RecipeId = 15, Name = "кисломолочний сир - 400-500г", Amount = "1 шт." },
                },
            };
            recipeRepoMock.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(recipe);

            var result = await service.GetSafeSubstitutesAsync(42, 15, 151);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value!.Substitutes);
            // Повинні бути тофу / рослинний сир
            Assert.Contains(result.Value.Substitutes, s => s.Name.Contains("Тофу"));
            // НІ В ЯКОМУ РАЗІ не повинно бути олій (кулінарна перевірка)
            Assert.DoesNotContain(result.Value.Substitutes, s => s.Name == "Кокосова олія");
            Assert.DoesNotContain(result.Value.Substitutes, s => s.Name == "Оливкова олія");
            Assert.DoesNotContain(result.Value.Substitutes, s => s.Name == "Рисове молоко");
            // Перевірка правильної ваги замість 1 шт.
            Assert.Equal("400-500г", result.Value.Substitutes[0].CalculatedAmount);
        }

        [Fact]
        public async Task GetSafeSubstitutes_Sugar_ReturnsErythritolAndSyrups()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "цукор" });

            var recipe = new Recipe
            {
                Id = 16,
                UserId = 10,
                IsPublic = true,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 161, RecipeId = 16, Name = "цукор - 3ст. л.", Amount = "1 шт." },
                },
            };
            recipeRepoMock.Setup(r => r.GetByIdAsync(16)).ReturnsAsync(recipe);

            var result = await service.GetSafeSubstitutesAsync(42, 16, 161);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value!.Substitutes);
            Assert.Contains(result.Value.Substitutes, s => s.Name == "Еритритол");
            Assert.Contains(result.Value.Substitutes, s => s.Name == "Сироп топінамбура");
            // Перевірка екстракції кількості
            Assert.Equal("3ст. л.", result.Value.Substitutes[0].CalculatedAmount);
        }

        [Fact]
        public async Task GetSafeSubstitutes_FailSafe_ExcludesUnverifiedSubstitutes()
        {
            var (service, db, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" });

            // Додаємо неперевірений замінник (IsVerified = false)
            db.IngredientSubstitutes.Add(new IngredientSubstitute
            {
                Id = 999,
                OriginalIngredientName = "молоко",
                SubstituteName = "Неперевірене молочко",
                Ratio = 1.0,
                IsVerified = false,
                AllergenCodes = string.Empty,
            });
            db.SaveChanges();

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            var result = await service.GetSafeSubstitutesAsync(42, 1, 101);

            Assert.True(result.IsSuccess);
            // Fail-safe правило повинно виключити неперевірений замінник
            Assert.DoesNotContain(result.Value!.Substitutes, s => s.Name == "Неперевірене молочко");
        }

        [Fact]
        public async Task GetSafeSubstitutes_CalculatesAmountCorrectlyWithRatio()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "лактоза" });

            var recipe = new Recipe
            {
                Id = 3,
                UserId = 10,
                IsPublic = true,
                Ingredients = new List<Ingredient>
                {
                    new Ingredient { Id = 301, RecipeId = 3, Name = "Вершкове масло", Amount = "100 г" },
                },
            };
            recipeRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(recipe);

            var result = await service.GetSafeSubstitutesAsync(42, 3, 301);
            Assert.True(result.IsSuccess);

            // Оливкова олія має ratio = 0.8 => 100 г * 0.8 = 80 г
            var oliveOil = result.Value!.Substitutes.FirstOrDefault(s => s.Name == "Оливкова олія");
            Assert.NotNull(oliveOil);
            Assert.Equal("80 г", oliveOil.CalculatedAmount);
        }

        [Fact]
        public async Task GetSafeSubstitutes_ReturnsFailure_WhenUnauthorized()
        {
            var (service, _, _, _) = CreateTestSetup();

            var result = await service.GetSafeSubstitutesAsync(0, 1, 101);

            Assert.True(result.IsFailure);
            Assert.Equal("Користувач не авторизований.", result.Error);
        }

        [Fact]
        public async Task GetSafeSubstitutes_ReturnsFailure_WhenRecipeNotFound()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup();
            recipeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Recipe?)null);

            var result = await service.GetSafeSubstitutesAsync(42, 999, 101);

            Assert.True(result.IsFailure);
            Assert.Equal("Рецепт не знайдено.", result.Error);
        }

        [Fact]
        public async Task GetSafeSubstitutes_ReturnsFailure_WhenPrivateRecipeNotOwnedByUser()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup();
            var privateRecipe = CreateTestRecipe(id: 1, userId: 10, isPublic: false);
            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(privateRecipe);

            // Користувач 42 намагається отримати доступ до приватного рецепта користувача 10
            var result = await service.GetSafeSubstitutesAsync(42, 1, 101);

            Assert.True(result.IsFailure);
            Assert.Equal("Цей рецепт приватний.", result.Error);
        }

        [Fact]
        public async Task ApplySubstitution_FailsIfSubstituteIsUnsafeForUser()
        {
            // Користувач має алергію на горіхи
            var (service, db, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "горіхи" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            // Замінник Id = 3 - Мигдальне молоко (містить горіхи)
            var result = await service.ApplySubstitutionAsync(42, 1, 101, 3);

            Assert.True(result.IsFailure);
            Assert.Contains("небезпечним", result.Error);
        }

        [Fact]
        public async Task ApplySubstitution_SucceedsAndPersists_WhenSubstituteIsSafe()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            // Замінник Id = 1 - Вівсяне молоко (безпечне)
            var applyRes = await service.ApplySubstitutionAsync(42, 1, 101, 1);

            Assert.True(applyRes.IsSuccess);
            Assert.Equal("Вівсяне молоко", applyRes.Value!.SubstituteIngredientName);

            // Перевіряємо, що збереглося в базі
            var userSubstitutions = await service.GetUserSubstitutionsAsync(42, 1);
            Assert.True(userSubstitutions.IsSuccess);
            Assert.Single(userSubstitutions.Value!);
            Assert.Equal("Вівсяне молоко", userSubstitutions.Value![0].SubstituteIngredientName);
        }

        [Fact]
        public async Task RemoveSubstitution_RemovesPersonalSubstitution()
        {
            var (service, _, recipeRepoMock, _) = CreateTestSetup(
                new Allergy { Id = 1, UserId = 42, AllergenName = "молоко" });

            recipeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestRecipe(1));

            await service.ApplySubstitutionAsync(42, 1, 101, 1);
            var beforeRemove = await service.GetUserSubstitutionsAsync(42, 1);
            Assert.Single(beforeRemove.Value!);

            var removeRes = await service.RemoveSubstitutionAsync(42, 1, 101);
            Assert.True(removeRes.IsSuccess);

            var afterRemove = await service.GetUserSubstitutionsAsync(42, 1);
            Assert.Empty(afterRemove.Value!);
        }
    }
}

