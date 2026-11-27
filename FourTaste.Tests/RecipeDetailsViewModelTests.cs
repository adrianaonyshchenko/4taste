using System.Collections.Generic;
using System.Linq;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using FourTaste.WPF.ViewModels;
using Xunit;

namespace FourTaste.Tests
{
    public class RecipeDetailsViewModelTests
    {
        [Fact]
        public void Commands_ShouldScaleIngredientsAndUpdateCanExecute()
        {
            var vm = new RecipeDetailsViewModel(new RecipeScalingService());
            var recipe = new Recipe
            {
                Id = 1,
                Servings = 2,
                Ingredients = new List<Ingredient>
                {
                    new() { Id = 10, Name = "Борошно", Amount = "500 г" },
                    new() { Id = 11, Name = "Сіль", Amount = "за смаком" },
                },
            };

            vm.Initialize(recipe, Enumerable.Empty<Ingredient>(), new Dictionary<int, AppliedSubstitutionDto>());

            Assert.Equal(2, vm.TargetServings);
            Assert.False(vm.ResetServingsCommand.CanExecute(null));
            Assert.True(vm.IncreaseServingsCommand.CanExecute(null));
            Assert.True(vm.DecreaseServingsCommand.CanExecute(null));

            vm.IncreaseServingsCommand.Execute(null);
            vm.IncreaseServingsCommand.Execute(null);

            Assert.Equal(4, vm.TargetServings);
            Assert.Equal("1 кг", vm.Ingredients[0].Amount);
            Assert.Equal("за смаком", vm.Ingredients[1].Amount);
            Assert.True(vm.ResetServingsCommand.CanExecute(null));

            vm.ResetServingsCommand.Execute(null);

            Assert.Equal(2, vm.TargetServings);
            Assert.Equal("500 г", vm.Ingredients[0].Amount);
            Assert.False(vm.ResetServingsCommand.CanExecute(null));
        }

        [Fact]
        public void TargetServings_ShouldScaleSubstitutedAllergenAmountToo()
        {
            var vm = new RecipeDetailsViewModel(new RecipeScalingService());
            var milk = new Ingredient { Id = 10, Name = "Молоко", Amount = "200 мл" };
            var recipe = new Recipe
            {
                Id = 1,
                Servings = 2,
                Ingredients = new List<Ingredient> { milk },
            };

            var substitutions = new Dictionary<int, AppliedSubstitutionDto>
            {
                [10] = new AppliedSubstitutionDto
                {
                    OriginalIngredientId = 10,
                    SubstituteIngredientName = "Вівсяне молоко",
                    CalculatedAmount = "200 мл",
                },
            };

            vm.Initialize(recipe, new[] { milk }, substitutions);
            vm.TargetServings = 6;

            Assert.Equal("Вівсяне молоко", vm.Ingredients[0].Name);
            Assert.Equal("600 мл", vm.Ingredients[0].Amount);
        }
    }
}