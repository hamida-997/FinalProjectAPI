using FinalProjectAPI.Helpers.DTOs.ProductImage;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImagesController : ControllerBase
    {
        private readonly IProductImageService _productImageService;
        private readonly IProductService _productService;
        private readonly IFileService _fileService;

        public ProductImagesController(
            IProductImageService productImageService,
            IProductService productService,
            IFileService fileService)
        {
            _productImageService = productImageService;
            _productService = productService;
            _fileService = fileService;
        }

        [HttpGet("Product/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductImageDto>>> GetImagesByProduct(int productId)
        {
            if (!await _productService.ProductExistsAsync(productId))
            {
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            var images = await _productImageService.GetImagesByProductIdAsync(productId);
            return Ok(images);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductImageDto>> GetProductImage(int id)
        {
            var image = await _productImageService.GetImageByIdAsync(id);

            if (image == null)
            {
                return NotFound(new { message = $"Product image with ID {id} not found." });
            }

            return Ok(image);
        }

        [HttpPost]
        public async Task<ActionResult<ProductImageDto>> CreateProductImage(CreateProductImageDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _productService.ProductExistsAsync(createDto.ProductId))
            {
                return BadRequest(new { message = $"Product with ID {createDto.ProductId} does not exist." });
            }

            var image = await _productImageService.CreateImageAsync(createDto);
            return CreatedAtAction(nameof(GetProductImage), new { id = image.Id }, image);
        }

        [HttpPost("Upload")]
        public async Task<ActionResult<ProductImageDto>> UploadProductImage(
            [FromForm] IFormFile file,
            [FromForm] int productId,
            [FromForm] bool isPrimary = false)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file was provided." });
            }

            if (!await _productService.ProductExistsAsync(productId))
            {
                return BadRequest(new { message = $"Product with ID {productId} does not exist." });
            }

            try
            {
                var fileUrl = await _fileService.SaveFileAsync(file, "products");

                var createDto = new CreateProductImageDto
                {
                    ProductId = productId,
                    ImageUrl = fileUrl,
                    IsPrimary = isPrimary
                };

                var image = await _productImageService.CreateImageAsync(createDto);
                return CreatedAtAction(nameof(GetProductImage), new { id = image.Id }, image);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductImageDto>> UpdateProductImage(int id, UpdateProductImageDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var image = await _productImageService.UpdateImageAsync(id, updateDto);

            if (image == null)
            {
                return NotFound(new { message = $"Product image with ID {id} not found." });
            }

            return Ok(image);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            var result = await _productImageService.DeleteImageAsync(id);

            if (!result)
            {
                return NotFound(new { message = $"Product image with ID {id} not found." });
            }

            return NoContent();
        }

        [HttpPut("{id}/SetPrimary")]
        public async Task<IActionResult> SetPrimaryImage(int id, [FromQuery] int productId)
        {
            var result = await _productImageService.SetPrimaryImageAsync(productId, id);

            if (!result)
            {
                return NotFound(new { message = $"Product image with ID {id} not found for product {productId}." });
            }

            return Ok(new { message = "Primary image set successfully." });
        }
    }
}
