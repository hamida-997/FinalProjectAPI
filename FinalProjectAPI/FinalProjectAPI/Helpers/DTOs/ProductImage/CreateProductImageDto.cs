using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.ProductImage
{
    public class CreateProductImageDto
    {
        [Required(ErrorMessage = "Image URL is required.")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please provide a valid URL.")]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        [Required(ErrorMessage = "Product is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }
    }
}
