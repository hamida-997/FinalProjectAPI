using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.ProductImage
{
    // Multipart form for image uploads. Swagger can only describe IFormFile
    // when it is part of a [FromForm] model, not a loose parameter.
    public class UploadProductImageDto
    {
        [Required(ErrorMessage = "No file was provided.")]
        public IFormFile File { get; set; } = null!;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        public bool IsPrimary { get; set; }
    }
}
