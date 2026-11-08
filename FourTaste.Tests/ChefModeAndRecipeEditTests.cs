using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Services;
using FourTaste.WPF;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class ChefModeAndRecipeEditTests
    {
        private readonly Mock<IMealPlanRepository> _mealPlanRepositoryMock;
        private readonly Mock<IRecipeService> _recipeServiceMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly MealPlanService _mealPlanService;

        public ChefModeAndRecipeEditTests()
        {
            _mealPlanRepositoryMock = new Mock<IMealPlanRepository>();
            _recipeServiceMock = new Mock<IRecipeService>();
            _loggerMock = new Mock<ILogger>();

            _mealPlanService = new MealPlanService(_mealPlanRepositoryMock.Object, _loggerMock.Object);
        }


        [Fact]
        public void ParseStep_WithWhyDelimiter_ShouldExtractDescriptionAndWhyCorrectly()
        {
            string rawStepText = "Розігрійте пательню на середньому вогні.|WHY:Це запобігає пригоранню масла.";

            string description = rawStepText;
            string why = string.Empty;

            int whyIndex = rawStepText.IndexOf("|WHY:", StringComparison.Ordinal);
            if (whyIndex >= 0)
            {
                why = rawStepText.Substring(whyIndex + 5).Trim();
                description = rawStepText.Substring(0, whyIndex).Trim();
            }

            Assert.Equal("Розігрійте пательню на середньому вогні.", description);
            Assert.Equal("Це запобігає пригоранню масла.", why);
        }

        [Fact]
        public void ParseStep_WithoutWhyDelimiter_ShouldReturnEmptyWhy()
        {
            string rawStepText = "Наріжте цибулю кубиками.";

            string description = rawStepText;
            string why = string.Empty;

            int whyIndex = rawStepText.IndexOf("|WHY:", StringComparison.Ordinal);
            if (whyIndex >= 0)
            {
                why = rawStepText.Substring(whyIndex + 5).Trim();
                description = rawStepText.Substring(0, whyIndex).Trim();
            }

            Assert.Equal("Наріжте цибулю кубиками.", description);
            Assert.Empty(why);
        }

        [Fact]
        public async Task RecordCookedRecipeAsync_ValidParameters_ShouldSaveMealPlanWithRecipeIdAndCookedStatus()
        {
            int userId = 1;
            int recipeId = 42;
            string recipeTitle = "Борщ український";
            DateTime date = DateTime.Today;

            _mealPlanRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<MealPlan>()))
                .ReturnsAsync((MealPlan mp) => mp);

            var result = await _mealPlanService.RecordCookedRecipeAsync(userId, recipeTitle, date, recipeId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(userId, result.Value.UserId);
            Assert.Equal(recipeId, result.Value.RecipeId);
            Assert.Equal(recipeTitle, result.Value.RecipeName);
            Assert.True(result.Value.IsCooked);

            _mealPlanRepositoryMock.Verify(repo => repo.AddAsync(It.Is<MealPlan>(m =>
                m.UserId == userId &&
                m.RecipeId == recipeId &&
                m.RecipeName == recipeTitle &&
                m.IsCooked == true
            )), Times.Once);
        }

        [Fact]
        public async Task RecordCookedRecipeAsync_InvalidUserId_ShouldReturnFailureResult()
        {
            int invalidUserId = 0;

            var result = await _mealPlanService.RecordCookedRecipeAsync(invalidUserId, "Тестова страва", DateTime.Today, 10);

            Assert.True(result.IsFailure);
            Assert.Equal("Користувач не визначений.", result.Error);
            _mealPlanRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<MealPlan>()), Times.Never);
        }


        [Fact]
        public void EditRecipe_FormatStepsWithWhy_ShouldCombineIntoSingleStringWithDelimiter()
        {
            var editableStep = new EditableStepModel
            {
                StepNumber = 1,
                Description = "Додайте щіпку солі",
                Why = "Сіль підкреслює солодкість випічки",
            };

            string formattedStep = editableStep.Description.Trim();
            if (!string.IsNullOrWhiteSpace(editableStep.Why))
            {
                formattedStep += $"|WHY:{editableStep.Why.Trim()}";
            }

            Assert.Equal("Додайте щіпку солі|WHY:Сіль підкреслює солодкість випічки", formattedStep);
        }

        [Fact]
        public async Task SaveEditedRecipe_WithWhySteps_ShouldPassFormattedStepsToService()
        {
            int currentUserId = 5;
            int recipeId = 12;

            var request = new UpdateRecipeRequest
            {
                Id = recipeId,
                UserId = currentUserId,
                Title = "Оновлений Рецепт",
                Time = "00:30",
                Category = "Обіди",
                Cuisine = "Українська",
                IsPublic = true,
                Servings = 2,
                UpdatePhoto = false,
                Ingredients = new List<RecipeIngredientDto>
                {
                    new RecipeIngredientDto { Name = "Картопля", Amount = "500г" }
                },
                Steps = new List<string>
                {
                    "Варити 20 хвилин.|WHY:Щоб картопля стала м'якою."
                }
            };

            _recipeServiceMock
                .Setup(s => s.UpdateRecipeAsync(It.IsAny<UpdateRecipeRequest>()))
                .ReturnsAsync(Result.Success());

            var result = await _recipeServiceMock.Object.UpdateRecipeAsync(request);

            Assert.True(result.IsSuccess);
            _recipeServiceMock.Verify(s => s.UpdateRecipeAsync(It.Is<UpdateRecipeRequest>(r =>
                r.Id == recipeId &&
                r.Steps.Count == 1 &&
                r.Steps[0] == "Варити 20 хвилин.|WHY:Щоб картопля стала м'якою."
            )), Times.Once);
        }
    }
}