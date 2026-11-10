using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.Review
{
    public class CreateReviewDto
    {
        [Required(ErrorMessage = "Product is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Rating is required.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }
    }
}
