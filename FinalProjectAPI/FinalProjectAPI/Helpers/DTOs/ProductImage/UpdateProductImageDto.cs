using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.ProductImage
{
    public class UpdateProductImageDto
    {
        [Required(ErrorMessage = "Image URL is required.")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL.")]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }
    }
}
