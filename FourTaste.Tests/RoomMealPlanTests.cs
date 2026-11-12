using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.ApplicationCore.Services;
using Moq;
using Serilog;
using Xunit;

namespace FourTaste.Tests
{
    public class RoomMealPlanServiceTests
    {
        [Fact]
        public async Task GetRoomMealPlansAsync_ShouldReturnPlansList_WhenRoomIdIsValid()
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var expectedPlans = new List<RoomMealPlan>
            {
                new RoomMealPlan { Id = 1, RoomId = 5, RecipeName = "Борщ", PlannedDate = DateTime.UtcNow },
                new RoomMealPlan { Id = 2, RoomId = 5, RecipeName = "Вареники", PlannedDate = DateTime.UtcNow }
            };

            mockRepo.Setup(r => r.GetByRoomIdAsync(5)).ReturnsAsync(expectedPlans);

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.GetRoomMealPlansAsync(5);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Equal("Борщ", result.Value[0].RecipeName);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 5), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task GetRoomMealPlansAsync_ShouldReturnFailure_WhenRoomIdIsInvalid(int invalidRoomId)
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.GetRoomMealPlansAsync(invalidRoomId);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректний ідентифікатор кімнати.", result.Error);
            mockRepo.Verify(r => r.GetByRoomIdAsync(It.IsAny<int>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), invalidRoomId), Times.Once);
        }

        [Fact]
        public async Task AddRoomMealPlanAsync_ShouldAddPlan_WhenDataIsValid()
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);
            var date = DateTime.Today;
            int testRecipeId = 99; 

            var result = await service.AddRoomMealPlanAsync(1, 2, "Оладки", testRecipeId, date);

            Assert.True(result.IsSuccess);

            mockRepo.Verify(r => r.AddAsync(It.Is<RoomMealPlan>(p =>
                p.RoomId == 1 &&
                p.UserId == 2 &&
                p.RecipeName == "Оладки" &&
                p.RecipeId == testRecipeId)), Times.Once);

            mockLogger.Verify(l => l.Information(It.IsAny<string>(), "Оладки", 1, It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData(0, 1, "Суп")]
        [InlineData(1, 0, "Суп")]
        [InlineData(-1, 2, "Суп")]
        public async Task AddRoomMealPlanAsync_ShouldReturnFailure_WhenIdsAreInvalid(int roomId, int userId, string recipeName)
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.AddRoomMealPlanAsync(roomId, userId, recipeName, null, DateTime.Today);

            Assert.True(result.IsFailure);
            Assert.Equal("Некоректні дані запиту.", result.Error);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<RoomMealPlan>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), roomId, userId), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddRoomMealPlanAsync_ShouldReturnFailure_WhenRecipeNameIsEmpty(string? invalidRecipeName)
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.AddRoomMealPlanAsync(1, 1, invalidRecipeName!, null, DateTime.Today);

            Assert.True(result.IsFailure);
            Assert.Equal("Назва страви не може бути порожньою.", result.Error);
            mockRepo.Verify(r => r.AddAsync(It.IsAny<RoomMealPlan>()), Times.Never);
            mockLogger.Verify(l => l.Warning(It.IsAny<string>(), 1), Times.Once);
        }

        [Fact]
        public async Task ToggleIsCookedAsync_ShouldToggleStatus_WhenPlanExists()
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var plan = new RoomMealPlan { Id = 10, RoomId = 1, UserId = 1, RecipeName = "Піца", IsCooked = false };
            mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(plan);

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.ToggleIsCookedAsync(10, 1);

            Assert.True(result.IsSuccess);
            Assert.True(plan.IsCooked);
            mockRepo.Verify(r => r.UpdateAsync(plan), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 10, true, 1), Times.Once);
        }

        [Fact]
        public async Task DeleteRoomMealPlanAsync_ShouldDeletePlan_WhenPlanExists()
        {
            var mockRepo = new Mock<IRoomMealPlanRepository>();
            var mockLogger = new Mock<ILogger>();

            var plan = new RoomMealPlan { Id = 15, RoomId = 1, UserId = 1, RecipeName = "Салат" };
            mockRepo.Setup(r => r.GetByIdAsync(15)).ReturnsAsync(plan);

            var service = new RoomMealPlanService(mockRepo.Object, mockLogger.Object);

            var result = await service.DeleteRoomMealPlanAsync(15, 1);

            Assert.True(result.IsSuccess);
            mockRepo.Verify(r => r.DeleteAsync(15), Times.Once);
            mockLogger.Verify(l => l.Information(It.IsAny<string>(), 15, 1), Times.Once);
        }
    }
}