using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
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

            var expectedPlan = new MealPlan { Id = 1, UserId = userId, RecipeName = recipeName, PlannedDate = targetDate };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<MealPlan>())).ReturnsAsync(expectedPlan);

            var result = await _service.AddCustomRecipeToPlanAsync(userId, recipeName, targetDate);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(recipeName, result.Value.RecipeName);
            Assert.Equal(userId, result.Value.UserId);
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
        public async Task AddRecipeToPlanAsync_WhenRepositoryThrows_ShouldPropagateException()
        {
            int userId = 1;
            string recipeName = "Борщ";
            DateTime targetDate = DateTime.Today;

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<MealPlan>())).ThrowsAsync(new Exception("Database error"));

            await Assert.ThrowsAsync<Exception>(() =>
                _service.AddCustomRecipeToPlanAsync(userId, recipeName, targetDate));
        }

        [Fact]
        public async Task UpdateMealPlanStatusAsync_ShouldReturnSuccess_WhenDataIsValid()
        {
            var existingPlan = new MealPlan { Id = 1, UserId = 1, RecipeName = "Борщ", IsCooked = false };
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingPlan);

            var result = await _service.UpdateMealPlanStatusAsync(1, 1, true);

            Assert.True(result.IsSuccess);
            _mockRepository.Verify(r => r.UpdateStatusAsync(1, true), Times.Once);
        }

        [Fact]
        public async Task UpdateMealPlanStatusAsync_ShouldReturnFailure_WhenMealPlanNotFound()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((MealPlan?)null);

            var result = await _service.UpdateMealPlanStatusAsync(999, 1, true);

            Assert.True(result.IsFailure);
            Assert.Equal("Запис меню не знайдено.", result.Error);
        }

        [Fact]
        public async Task UpdateMealPlanStatusAsync_ShouldReturnFailure_WhenPlanBelongsToAnotherUser()
        {
            var existingPlan = new MealPlan { Id = 1, UserId = 2, RecipeName = "Борщ", IsCooked = false };
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingPlan);

            var result = await _service.UpdateMealPlanStatusAsync(1, 1, true);

            Assert.True(result.IsFailure);
            Assert.Equal("Немає доступу до цього запису меню.", result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task UpdateMealPlanStatusAsync_ShouldReturnFailure_WhenMealPlanIdIsInvalid(int invalidId)
        {
            var result = await _service.UpdateMealPlanStatusAsync(invalidId, 1, true);

            Assert.True(result.IsFailure);
        }

        [Fact]
        public async Task UpdateMealPlanStatusAsync_ShouldNotCallRepository_WhenStatusIsAlreadySet()
        {
            var existingPlan = new MealPlan { Id = 1, UserId = 1, RecipeName = "Борщ", IsCooked = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingPlan);

            var result = await _service.UpdateMealPlanStatusAsync(1, 1, true);

            Assert.True(result.IsSuccess);
            _mockRepository.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetCookingHistoryAsync_ShouldReturnCookedDishes_WhenValidUserId()
        {
            int userId = 1;
            var history = new List<MealPlan>
            {
                new MealPlan { Id = 1, UserId = userId, RecipeName = "Паста", IsCooked = true, PlannedDate = DateTime.Today.AddDays(-1) },
                new MealPlan { Id = 2, UserId = userId, RecipeName = "Піца", IsCooked = true, PlannedDate = DateTime.Today }
            };

            _mockRepository.Setup(r => r.GetCookingHistoryAsync(userId, null)).ReturnsAsync(history);

            var result = await _service.GetCookingHistoryAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.All(result.Value, p => Assert.True(p.IsCooked));
            _mockRepository.Verify(r => r.GetCookingHistoryAsync(userId, null), Times.Once);
        }

        [Fact]
        public async Task GetCookingHistoryAsync_ShouldFilterByDate_WhenDateIsSpecified()
        {
            int userId = 1;
            var targetDate = new DateTime(2026, 10, 18);
            var history = new List<MealPlan>
            {
                new MealPlan { Id = 2, UserId = userId, RecipeName = "Піца", IsCooked = true, PlannedDate = targetDate }
            };

            _mockRepository.Setup(r => r.GetCookingHistoryAsync(userId, targetDate)).ReturnsAsync(history);

            var result = await _service.GetCookingHistoryAsync(userId, targetDate);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("Піца", result.Value[0].RecipeName);
            _mockRepository.Verify(r => r.GetCookingHistoryAsync(userId, targetDate), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetCookingHistoryAsync_ShouldReturnFailure_WhenUserIdIsInvalid(int invalidUserId)
        {
            var result = await _service.GetCookingHistoryAsync(invalidUserId);

            Assert.True(result.IsFailure);
            Assert.Equal("Користувач не визначений.", result.Error);
            _mockRepository.Verify(r => r.GetCookingHistoryAsync(It.IsAny<int>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task GetCookingHistoryAsync_ShouldReturnEmptyList_WhenNoDishesCooked()
        {
            int userId = 3;
            _mockRepository.Setup(r => r.GetCookingHistoryAsync(userId, null)).ReturnsAsync(new List<MealPlan>());

            var result = await _service.GetCookingHistoryAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }
    }
}