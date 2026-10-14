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

            _mapper.Map(updateDto, category);

            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }
    }
}
