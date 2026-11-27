using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeScalingService
    {
        Result<string> ScaleAmount(string? rawAmount, int baseServings, int targetServings);
    }
}