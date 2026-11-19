using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Services;
using Xunit;

namespace FourTaste.Tests
{
    public class CreateRoomRequestValidatorTests
    {
        [Fact]
        public void Validate_ShouldSucceed_WhenRequestIsValid()
        {
            var result = CreateRoomRequestValidator.Validate(new CreateRoomRequest { Name = "Кухня", Description = "Опис" });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Validate_ShouldSucceed_WhenDescriptionIsMissing()
        {
            var result = CreateRoomRequestValidator.Validate(new CreateRoomRequest { Name = "Кухня", Description = null });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Validate_ShouldSucceed_WhenLengthsAreExactlyAtLimit()
        {
            var request = new CreateRoomRequest
            {
                Name = new string('N', RoomOptions.MaxNameLength),
                Description = new string('D', RoomOptions.MaxDescriptionLength),
            };

            Assert.True(CreateRoomRequestValidator.Validate(request).IsSuccess);
        }

        [Fact]
        public void Validate_ShouldFail_WhenRequestIsNull()
        {
            var result = CreateRoomRequestValidator.Validate(null);

            Assert.True(result.IsFailure);
            Assert.Equal("Дані кімнати не задано.", result.Error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_ShouldFail_WhenNameIsEmpty(string? name)
        {
            var result = CreateRoomRequestValidator.Validate(new CreateRoomRequest { Name = name! });

            Assert.True(result.IsFailure);
            Assert.Equal("Назва кімнати не може бути порожньою.", result.Error);
        }

        [Fact]
        public void Validate_ShouldIgnoreSurroundingSpaces_WhenCheckingNameLength()
        {
            var request = new CreateRoomRequest { Name = "  " + new string('N', RoomOptions.MaxNameLength) + "  " };

            Assert.True(CreateRoomRequestValidator.Validate(request).IsSuccess);
        }

        [Fact]
        public void Validate_ShouldFail_WhenNameIsTooLong()
        {
            var result = CreateRoomRequestValidator.Validate(new CreateRoomRequest { Name = new string('N', RoomOptions.MaxNameLength + 1) });

            Assert.True(result.IsFailure);
            Assert.Equal($"Назва кімнати не може перевищувати {RoomOptions.MaxNameLength} символів.", result.Error);
        }

        [Fact]
        public void Validate_ShouldFail_WhenDescriptionIsTooLong()
        {
            var request = new CreateRoomRequest
            {
                Name = "Кухня",
                Description = new string('D', RoomOptions.MaxDescriptionLength + 1),
            };

            var result = CreateRoomRequestValidator.Validate(request);

            Assert.True(result.IsFailure);
            Assert.Equal($"Опис кімнати не може перевищувати {RoomOptions.MaxDescriptionLength} символів.", result.Error);
        }

        [Fact]
        public void Validate_ShouldReturnFirstError_WhenSeveralRulesFail()
        {
            var request = new CreateRoomRequest
            {
                Name = "   ",
                Description = new string('D', RoomOptions.MaxDescriptionLength + 1),
            };

            var result = CreateRoomRequestValidator.Validate(request);

            Assert.Equal("Назва кімнати не може бути порожньою.", result.Error);
        }
    }
}
