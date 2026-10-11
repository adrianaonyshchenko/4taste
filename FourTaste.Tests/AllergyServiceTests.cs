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
    public class AllergyServiceTests
    {
        [Fact]
        public async Task AddAllergyAsync_ShouldSaveAllergyAndLogInformation_WhenAllergenNameIsValid()
        {
            var mockRepo = new Mock<IRepository<Allergy>>();
            var mockLogger = new Mock<ILogger>();
            var service = new AllergyService(mockRepo.Object, mockLogger.Object);

            await service.AddAllergyAsync(1, "Арахіс");

            mockRepo.Verify(
                r => r.AddAsync(It.Is<Allergy>(a => a.UserId == 1 && a.AllergenName == "Арахіс")),
                Times.Once);
            mockLogger.Verify(
                l => l.Information(It.IsAny<string>(), "Арахіс", 1),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddAllergyAsync_ShouldThrowArgumentExceptionAndLogWarning_WhenAllergenNameIsEmpty(string invalidName)
        {
            var mockRepo = new Mock<IRepository<Allergy>>();
            var mockLogger = new Mock<ILogger>();
            var service = new AllergyService(mockRepo.Object, mockLogger.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddAllergyAsync(1, invalidName));

            mockRepo.Verify(r => r.AddAsync(It.IsAny<Allergy>()), Times.Never);
            mockLogger.Verify(
                l => l.Warning(It.IsAny<string>(), 1),
                Times.Once);
        }

        [Fact]
        public async Task GetAllergiesByUserIdAsync_ShouldReturnFilteredAllergiesAndLogInformation()
        {
            var allAllergies = new List<Allergy>
            {
                new Allergy { Id = 1, UserId = 1, AllergenName = "Арахіс" },
                new Allergy { Id = 2, UserId = 2, AllergenName = "Мед" },
                new Allergy { Id = 3, UserId = 1, AllergenName = "Лактоза" },
            };

            var mockRepo = new Mock<IRepository<Allergy>>();
            mockRepo.Setup(r => r.ListAsync()).ReturnsAsync(allAllergies);
            var mockLogger = new Mock<ILogger>();
            var service = new AllergyService(mockRepo.Object, mockLogger.Object);

            var result = await service.GetAllergiesByUserIdAsync(1);

            Assert.Equal(2, result.Count);
            mockLogger.Verify(
                l => l.Information(It.IsAny<string>(), 2, 1),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllergyAsync_ShouldRemoveAllergyAndLogInformation_WhenAllergyExists()
        {
            var existing = new Allergy { Id = 5, UserId = 1, AllergenName = "Глютен" };
            var mockRepo = new Mock<IRepository<Allergy>>();
            mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
            var mockLogger = new Mock<ILogger>();
            var service = new AllergyService(mockRepo.Object, mockLogger.Object);

            await service.DeleteAllergyAsync(5);

            mockRepo.Verify(r => r.DeleteAsync(existing), Times.Once);
            mockLogger.Verify(
                l => l.Information(It.IsAny<string>(), "Глютен", 5),
                Times.Once);
        }
    }
}
