using AutoMapper;
using FinalProjectAPI.Data;
using FinalProjectAPI.Helpers.DTOs.Category;
using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public CategoryService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto)
        {
            createDto.Name = createDto.Name.Trim();
            await EnsureNameIsFreeAsync(createDto.Name, excludeId: null);

            var category = _mapper.Map<Category>(createDto);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return null;

            updateDto.Name = updateDto.Name.Trim();
            await EnsureNameIsFreeAsync(updateDto.Name, excludeId: id);

            _mapper.Map(updateDto, category);

            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            // Products reference the category (Restrict), so deleting would fail with a 500
            var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);
            if (productCount > 0)
            {
                throw new InvalidOperationException(
                    $"'{category.Name}' still has {productCount} product{(productCount == 1 ? "" : "s")}. Move or delete them first, or mark the category inactive.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }

        // Names have a unique index; check first so the client gets a clear 400 instead of a 500
        private async Task EnsureNameIsFreeAsync(string name, int? excludeId)
        {
            var taken = await _context.Categories
                .AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));

            if (taken)
                throw new InvalidOperationException($"A category named '{name}' already exists.");
        }
    }
}
