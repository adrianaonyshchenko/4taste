using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.UnitTests
{
    public class MealPlanServiceTests
    {
        private readonly Mock<IMealPlanRepository> _mockRepository;
        private readonly Mock<ILogger> _mockLogger;
        private readonly MealPlanService _service;

        public MealPlanServiceTests()
        {
            _mockRepository = new Mock<IMealPlanRepository>();
            _mockLogger = new Mock<ILogger>();

            _service = new MealPlanService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddRecipeToPlanAsync_ValidData_ShouldReturnCreatedMealPlan()
        {
            int userId = 1;
            string recipeName = "Борщ";
            DateTime targetDate = new DateTime(2026, 9, 4);

            var expectedPlan = new MealPlan
            {
                Id = 1,
                UserId = userId,
                RecipeName = recipeName,
                PlannedDate = targetDate
            };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<MealPlan>()))
                           .ReturnsAsync(expectedPlan);

            var result = await _service.AddCustomRecipeToPlanAsync(userId, recipeName, targetDate);

            Assert.NotNull(result);
            Assert.Equal(recipeName, result.RecipeName);
            Assert.Equal(userId, result.UserId);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<MealPlan>()), Times.Once);
        }

        [Fact]
        public async Task GetUserPlansForDateAsync_ExistingPlans_ShouldReturnList()
        {
            int userId = 2;
            DateTime targetDate = new DateTime(2026, 9, 4);

            var existingPlans = new List<MealPlan>
            {
                new MealPlan { Id = 1, UserId = userId, RecipeName = "Борщ", PlannedDate = targetDate }
            };

            _mockRepository.Setup(r => r.GetPlansByUserAndDateAsync(userId, targetDate))
                           .ReturnsAsync(existingPlans);

            var result = await _service.GetUserPlansForDateAsync(userId, targetDate);

            Assert.NotNull(result);
            Assert.Single(result);
            _mockRepository.Verify(r => r.GetPlansByUserAndDateAsync(userId, targetDate), Times.Once);
        }

        [Fact]
        public async Task AddRecipeToPlanAsync_WhenErrorOccurs_ShouldThrowException()
        {
            int userId = 1;
            string recipeName = "Борщ";
            DateTime targetDate = DateTime.Today;

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<MealPlan>()))
                           .ThrowsAsync(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.AddCustomRecipeToPlanAsync(userId, recipeName, targetDate));

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<MealPlan>()), Times.Once);
        }
    }
}