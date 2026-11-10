using FinalProjectAPI.Data;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.Review;
using FinalProjectAPI.Helpers.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsForProductAsync(int productId)
        {
            return await ProjectToDto(_context.Reviews.Where(r => r.ProductId == productId))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // For the home page: good reviews (4-5 stars) that have a comment, newest first
        public async Task<IEnumerable<ReviewDto>> GetFeaturedReviewsAsync(int count)
        {
            return await ProjectToDto(_context.Reviews.Where(r =>
                    r.Rating >= 4 && r.Comment != null && r.Comment != ""))
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReviewDto>> GetAllReviewsAsync(int? productId = null)
        {
            var query = _context.Reviews.AsQueryable();
            if (productId.HasValue)
            {
                query = query.Where(r => r.ProductId == productId.Value);
            }

            return await ProjectToDto(query)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ReviewDto?> GetReviewByIdAsync(int id)
        {
            return await ProjectToDto(_context.Reviews.Where(r => r.Id == id))
                .FirstOrDefaultAsync();
        }

        public async Task<ReviewEligibilityDto> GetEligibilityAsync(int productId, string userId)
        {
            var existingId = await _context.Reviews
                .Where(r => r.ProductId == productId && r.UserId == userId)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();

            return new ReviewEligibilityDto
            {
                HasPurchased = await HasPurchasedAsync(productId, userId),
                ExistingReviewId = existingId
            };
        }

        public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, string userId)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == dto.ProductId))
                throw new InvalidOperationException($"Product with ID {dto.ProductId} does not exist.");

            if (!await HasPurchasedAsync(dto.ProductId, userId))
                throw new InvalidOperationException("You can only review products you have bought.");

            if (await _context.Reviews.AnyAsync(r => r.ProductId == dto.ProductId && r.UserId == userId))
                throw new InvalidOperationException("You have already reviewed this product. Edit your existing review instead.");

            var review = new Review
            {
                ProductId = dto.ProductId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = CleanComment(dto.Comment),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two requests at once: the unique (ProductId, UserId) index rejects the second
                throw new InvalidOperationException("You have already reviewed this product. Edit your existing review instead.");
            }

            return (await GetReviewByIdAsync(review.Id))!;
        }

        public async Task<ReviewDto?> UpdateReviewAsync(int id, UpdateReviewDto dto, string userId)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (review == null)
                return null;

            review.Rating = dto.Rating;
            review.Comment = CleanComment(dto.Comment);
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetReviewByIdAsync(id);
        }

        public async Task<bool> DeleteReviewAsync(int id, string userId, bool isAdmin)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review == null || (!isAdmin && review.UserId != userId))
                return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        // A "buyer" has a paid, non-cancelled order that contains the product
        private Task<bool> HasPurchasedAsync(int productId, string userId)
        {
            return _context.Orders.AnyAsync(o =>
                o.UserId == userId &&
                o.PaymentStatus == PaymentStatus.Paid &&
                o.Status != OrderStatus.Cancelled &&
                o.OrderItems.Any(oi => oi.ProductId == productId));
        }

        private static string? CleanComment(string? comment)
        {
            var trimmed = comment?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        // Runs in SQL; shows first name and last initial instead of the email
        private static IQueryable<ReviewDto> ProjectToDto(IQueryable<Review> query)
        {
            return query.Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                UserId = r.UserId,
                ReviewerName = r.User.LastName.Length > 0
                    ? r.User.FirstName + " " + r.User.LastName.Substring(0, 1) + "."
                    : r.User.FirstName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            });
        }
    }
}
