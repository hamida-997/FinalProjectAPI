using FinalProjectAPI.Data;
using FinalProjectAPI.DTOs;
using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class ProductImageService : IProductImageService
    {
        private readonly AppDbContext _context;

        public ProductImageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductImageDto>> GetImagesByProductIdAsync(int productId)
        {
            var images = await _context.ProductImages
                .Where(img => img.ProductId == productId)
                .OrderByDescending(img => img.IsPrimary)
                .ThenBy(img => img.CreatedAt)
                .ToListAsync();

            return images.Select(img => MapToDto(img));
        }

        public async Task<ProductImageDto?> GetImageByIdAsync(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            return image == null ? null : MapToDto(image);
        }

        public async Task<ProductImageDto> CreateImageAsync(CreateProductImageDto createDto)
        {
            // If this image is set as primary, unset other primary images for the product
            if (createDto.IsPrimary)
            {
                await UnsetPrimaryImagesAsync(createDto.ProductId);
            }

            var image = new ProductImage
            {
                ImageUrl = createDto.ImageUrl,
                IsPrimary = createDto.IsPrimary,
                ProductId = createDto.ProductId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            return MapToDto(image);
        }

        public async Task<ProductImageDto?> UpdateImageAsync(int id, UpdateProductImageDto updateDto)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
                return null;

            // If this image is being set as primary, unset other primary images for the product
            if (updateDto.IsPrimary && !image.IsPrimary)
            {
                await UnsetPrimaryImagesAsync(image.ProductId);
            }

            image.ImageUrl = updateDto.ImageUrl;
            image.IsPrimary = updateDto.IsPrimary;

            await _context.SaveChangesAsync();

            return MapToDto(image);
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
                return false;

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(int productId, int imageId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(img => img.Id == imageId && img.ProductId == productId);

            if (image == null)
                return false;

            // Unset all primary images for this product
            await UnsetPrimaryImagesAsync(productId);

            // Set this image as primary
            image.IsPrimary = true;
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task UnsetPrimaryImagesAsync(int productId)
        {
            var primaryImages = await _context.ProductImages
                .Where(img => img.ProductId == productId && img.IsPrimary)
                .ToListAsync();

            foreach (var img in primaryImages)
            {
                img.IsPrimary = false;
            }

            if (primaryImages.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private static ProductImageDto MapToDto(ProductImage image)
        {
            return new ProductImageDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                ProductId = image.ProductId,
                CreatedAt = image.CreatedAt
            };
        }
    }
}
