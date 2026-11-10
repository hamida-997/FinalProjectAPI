using AutoMapper;
using FinalProjectAPI.Data;
using FinalProjectAPI.Helpers.DTOs.Product;
using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public ProductService(AppDbContext context, IMapper mapper, IFileService fileService)
        {
            _context = context;
            _mapper = mapper;
            _fileService = fileService;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return await WithRatingsAsync(_mapper.Map<List<ProductDto>>(products));
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return await WithRatingsAsync(_mapper.Map<List<ProductDto>>(products));
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            var dto = _mapper.Map<ProductDto>(product);
            await WithRatingsAsync(new List<ProductDto> { dto });
            return dto;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
        {
            var product = _mapper.Map<Product>(createDto);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _context.Entry(product)
                .Reference(p => p.Category)
                .LoadAsync();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateDto)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return null;

            _mapper.Map(updateDto, product);

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<ProductDto>(product);
            await WithRatingsAsync(new List<ProductDto> { dto });
            return dto;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return false;

            // Order items reference the product (Restrict), so deleting would fail with a 500
            if (await _context.OrderItems.AnyAsync(oi => oi.ProductId == id))
            {
                throw new InvalidOperationException(
                    $"'{product.Name}' appears in past orders, so it can't be deleted. Untick \"Available for sale\" to hide it instead.");
            }

            var imageUrls = product.ProductImages.Select(i => i.ImageUrl).ToList();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Image rows are removed by cascade; also remove the files from wwwroot
            foreach (var url in imageUrls.Where(u => u.StartsWith("/uploads/")))
            {
                _fileService.DeleteFile(url);
            }

            return true;
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        // One grouped query for all products in the list, instead of one query per product
        private async Task<List<ProductDto>> WithRatingsAsync(List<ProductDto> products)
        {
            if (products.Count == 0)
                return products;

            var ids = products.Select(p => p.Id).ToList();

            var stats = await _context.Reviews
                .Where(r => ids.Contains(r.ProductId))
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .ToDictionaryAsync(s => s.ProductId);

            foreach (var product in products)
            {
                if (stats.TryGetValue(product.Id, out var s))
                {
                    product.ReviewCount = s.Count;
                    product.AverageRating = Math.Round(s.Average, 1);
                }
            }

            return products;
        }
    }
}
