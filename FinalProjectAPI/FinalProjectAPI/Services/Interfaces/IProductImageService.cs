using FinalProjectAPI.Helpers.DTOs.ProductImage;

namespace FinalProjectAPI.Services
{
    public interface IProductImageService
    {
        Task<IEnumerable<ProductImageDto>> GetImagesByProductIdAsync(int productId);
        Task<ProductImageDto?> GetImageByIdAsync(int id);
        Task<ProductImageDto> CreateImageAsync(CreateProductImageDto createDto);
        Task<ProductImageDto?> UpdateImageAsync(int id, UpdateProductImageDto updateDto);
        Task<bool> DeleteImageAsync(int id);
        Task<bool> SetPrimaryImageAsync(int productId, int imageId);
    }
}
