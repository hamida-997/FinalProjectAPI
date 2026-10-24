using AutoMapper;
using FinalProjectAPI.Data;
using FinalProjectAPI.Helpers.DTOs.ProductImage;
using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class ProductImageService : IProductImageService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public ProductImageService(AppDbContext context, IMapper mapper, IFileService fileService)
        {
            _context = context;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<IEnumerable<ProductImageDto>> GetImagesByProductIdAsync(int productId)
        {
            var images = await _context.ProductImages
                .Where(img => img.ProductId == productId)
                .OrderByDescending(img => img.IsPrimary)
                .ThenBy(img => img.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductImageDto>>(images);
        }

        public async Task<ProductImageDto?> GetImageByIdAsync(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            return image == null ? null : _mapper.Map<ProductImageDto>(image);
        }

        public async Task<ProductImageDto> CreateImageAsync(CreateProductImageDto createDto)
        {
            // If this image is set as primary, unset other primary images for the product
            if (createDto.IsPrimary)
            {
                await UnsetPrimaryImagesAsync(createDto.ProductId);
            }

            var image = _mapper.Map<ProductImage>(createDto);

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductImageDto>(image);
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

            _mapper.Map(updateDto, image);

            await _context.SaveChangesAsync();

            return _mapper.Map<ProductImageDto>(image);
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
                return false;

            // Save the image URL before removing from DB
            var imageUrl = image.ImageUrl;

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            // Delete the physical file from wwwroot
            // Only delete locally stored files (not external URLs)
            if (imageUrl.StartsWith("/uploads/"))
            {
                _fileService.DeleteFile(imageUrl);
            }

            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(int productId, int imageId)
        {
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(img => img.Id == imageId && img.ProductId == productId);

            if (image == null)
                return false;

            await UnsetPrimaryImagesAsync(productId);

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
    }
}
