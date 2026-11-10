namespace FinalProjectAPI.Helpers.DTOs.Review
{
    // Tells the product page whether to show the review form
    public class ReviewEligibilityDto
    {
        public bool HasPurchased { get; set; }

        // Set when the user already reviewed this product (they can edit it instead)
        public int? ExistingReviewId { get; set; }

        public bool CanReview => HasPurchased && ExistingReviewId == null;
    }
}
