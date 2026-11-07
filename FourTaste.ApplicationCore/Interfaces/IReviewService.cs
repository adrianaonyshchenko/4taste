using System.Collections.Generic;
using System.Threading.Tasks;
using FourTaste.ApplicationCore.Models;
using FourTaste.ApplicationCore.Results;

namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IReviewService
    {
        IReadOnlyList<string> GetAvailableTags();
        Task<Result<int>> AddReviewAsync(ReviewRequest request);
        Task<Result> UpdateReviewAsync(ReviewRequest request);
        Task<Result> DeleteReviewAsync(int recipeId, int userId);
        Task<Result<ReviewsOverview>> GetOverviewAsync(int recipeId, int? viewerUserId);
        Task<Result<Dictionary<int, byte[]>>> GetAuthorAvatarsAsync(IEnumerable<int> userIds);
    }
}