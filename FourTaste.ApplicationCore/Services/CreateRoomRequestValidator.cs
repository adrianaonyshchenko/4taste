using System;
using System.Linq;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Services
{
    public static class CreateRoomRequestValidator
    {
        public const string EmptyRequestError = "Дані кімнати не задано.";

        private static readonly (Func<CreateRoomRequest, bool> IsInvalid, string Error)[] Rules =
        {
            (r => string.IsNullOrWhiteSpace(r.Name), "Назва кімнати не може бути порожньою."),
            (r => r.Name.Trim().Length > RoomOptions.MaxNameLength, $"Назва кімнати не може перевищувати {RoomOptions.MaxNameLength} символів."),
            (r => (r.Description?.Trim().Length ?? 0) > RoomOptions.MaxDescriptionLength, $"Опис кімнати не може перевищувати {RoomOptions.MaxDescriptionLength} символів."),
        };

        public static Result Validate(CreateRoomRequest? request)
        {
            if (request is null)
            {
                return Result.Failure(EmptyRequestError);
            }

            string? error = Rules.FirstOrDefault(rule => rule.IsInvalid(request)).Error;
            return error is null ? Result.Success() : Result.Failure(error);
        }
    }
}
