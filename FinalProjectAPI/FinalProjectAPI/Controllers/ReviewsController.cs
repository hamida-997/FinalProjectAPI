using System.Security.Claims;
using FinalProjectAPI.Helpers.DTOs.Review;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // GET: api/Reviews/product/5  (public)
        [HttpGet("product/{productId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetProductReviews(int productId)
        {
            var reviews = await _reviewService.GetReviewsForProductAsync(productId);
            return Ok(reviews);
        }

        // GET: api/Reviews/5  (public)
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReviewDto>> GetReview(int id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
            {
                return NotFound(new { message = $"Review with ID {id} not found." });
            }

            return Ok(review);
        }

        // GET: api/Reviews?productId=5  (admin)
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAllReviews([FromQuery] int? productId = null)
        {
            var reviews = await _reviewService.GetAllReviewsAsync(productId);
            return Ok(reviews);
        }

        // GET: api/Reviews/product/5/eligibility
        // Whether the logged-in user can leave a review, or already has one
        [HttpGet("product/{productId:int}/eligibility")]
        public async Task<ActionResult<ReviewEligibilityDto>> GetEligibility(int productId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            return Ok(await _reviewService.GetEligibilityAsync(productId, userId));
        }

        // POST: api/Reviews
        [HttpPost]
        public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var review = await _reviewService.CreateReviewAsync(dto, userId);
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Reviews/5  (owner only)
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReviewDto>> UpdateReview(int id, UpdateReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var review = await _reviewService.UpdateReviewAsync(id, dto, userId);

            // 404 for someone else's review too, so IDs can't be probed
            if (review == null)
            {
                return NotFound(new { message = $"Review with ID {id} not found." });
            }

            return Ok(review);
        }

        // DELETE: api/Reviews/5  (owner or admin)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            var deleted = await _reviewService.DeleteReviewAsync(id, userId, isAdmin);

            if (!deleted)
            {
                return NotFound(new { message = $"Review with ID {id} not found." });
            }

            return NoContent();
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
