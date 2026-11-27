using FourTaste.ApplicationCore.Services;
using Xunit;

namespace FourTaste.Tests
{
    public class RecipeScalingServiceTests
    {
        private readonly RecipeScalingService service = new();

        [Theory]
        [InlineData("400 г", 2, 8, "1,6 кг")]
        [InlineData("1 кг", 4, 1, "250 г")]
        [InlineData("250 мл", 1, 4, "1 л")]
        [InlineData("1,5 л", 3, 1, "500 мл")]
        [InlineData("15 г", 3, 1, "5 г")]
        [InlineData("25 г", 3, 1, "8,3 г")]
        [InlineData("1 шт", 2, 3, "1,5 шт")]
        [InlineData("1 ст. л.", 2, 1, "0,5 ст. л.")]
        [InlineData("1 ч. л.", 4, 1, "0,25 ч. л.")]
        [InlineData("100–150 г", 2, 4, "200–300 г")]
        [InlineData("400-600 г", 2, 6, "1,2–1,8 кг")]
        [InlineData("за смаком", 2, 10, "за смаком")]
        [InlineData("щіпка", 1, 5, "щіпка")]
        public void ScaleAmount_ShouldConvertAndRoundAccordingToRules(
            string input, int baseServings, int targetServings, string expected)
        {
            var result = this.service.ScaleAmount(input, baseServings, targetServings);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Value);
        }

        [Fact]
        public void ScaleAmount_ShouldReturnOriginal_WhenServingsAreEqual()
        {
            var result = this.service.ScaleAmount("333 г", 4, 4);

            Assert.True(result.IsSuccess);
            Assert.Equal("333 г", result.Value);
        }

        [Fact]
        public void ScaleAmount_ShouldTreatZeroOrNegativeBaseServingsAsOne()
        {
            var result = this.service.ScaleAmount("200 г", 0, 3);

            Assert.True(result.IsSuccess);
            Assert.Equal("600 г", result.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(51)]
        public void ScaleAmount_ShouldReturnFailure_WhenTargetServingsOutOfRange(int invalidTarget)
        {
            var result = this.service.ScaleAmount("200 г", 2, invalidTarget);

            Assert.True(result.IsFailure);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ScaleAmount_ShouldReturnEmptySuccess_WhenAmountIsEmpty(string? rawAmount)
        {
            var result = this.service.ScaleAmount(rawAmount, 2, 4);

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.Value);
        }
    }
}