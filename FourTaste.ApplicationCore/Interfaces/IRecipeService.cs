using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeService
    {
        Task<Result> CreateRecipeAsync(string title, string time, string category, string cuisine, bool isPublic, int userId, List<string> ingredients, List<string> steps);
    }
}