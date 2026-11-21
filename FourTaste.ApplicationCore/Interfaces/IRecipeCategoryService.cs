using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IRecipeCategoryService
    {
        Task<Result<List<RecipeCategory>>> GetAllAsync();

        Task<Result<int>> CreateAsync(string name);

        Task<Result> RenameAsync(int id, string newName);

        Task<Result> DeleteAsync(int id);
    }
}