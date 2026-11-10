using FinalProjectAPI.Helpers.DTOs.Review;

namespace FinalProjectAPI.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(int productId);

        // Admin list, optionally filtered by product
        Task<IEnumerable<ReviewDto>> GetAllReviewsAsync(int? productId = null);

        Task<ReviewDto?> GetReviewByIdAsync(int id);

        Task<ReviewEligibilityDto> GetEligibilityAsync(int productId, string userId);

        /// <summary>
        /// Throws InvalidOperationException with a user-facing message if the product doesn't
        /// exist, the user hasn't bought it, or they already reviewed it.
        /// </summary>
        Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, string userId);

        // Returns null if the review doesn't exist or belongs to someone else
        Task<ReviewDto?> UpdateReviewAsync(int id, UpdateReviewDto dto, string userId);

        // Owners can delete their own review; admins can delete any
        Task<bool> DeleteReviewAsync(int id, string userId, bool isAdmin);
    }
}
