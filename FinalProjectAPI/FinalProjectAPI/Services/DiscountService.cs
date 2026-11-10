using FinalProjectAPI.Data;
using FinalProjectAPI.Entities;
using FinalProjectAPI.Helpers.DTOs.Discount;
using FinalProjectAPI.Helpers.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _context;

        public DiscountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DiscountValidationResultDto> ValidateAsync(string? code, decimal subtotal)
        {
            var normalized = Normalize(code);
            if (string.IsNullOrEmpty(normalized))
                return Invalid("Please enter a discount code.", subtotal);

            var discount = await _context.DiscountCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Code == normalized);

            // Same message for unknown and inactive codes, so codes can't be probed
            if (discount == null || !discount.IsActive)
                return Invalid("This discount code is not valid.", subtotal);

            if (discount.ExpiresAt.HasValue && discount.ExpiresAt.Value <= DateTime.UtcNow)
                return Invalid("This discount code has expired.", subtotal);

            if (discount.MaxUses.HasValue && discount.UsedCount >= discount.MaxUses.Value)
                return Invalid("This discount code has reached its usage limit.", subtotal);

            if (discount.MinOrderAmount.HasValue && subtotal < discount.MinOrderAmount.Value)
                return Invalid($"This code requires a minimum order of ${discount.MinOrderAmount.Value:F2}.", subtotal);

            var amount = CalculateDiscount(discount, subtotal);

            return new DiscountValidationResultDto
            {
                IsValid = true,
                Code = discount.Code,
                Subtotal = subtotal,
                DiscountAmount = amount,
                Total = subtotal - amount
            };
        }

        public async Task<DiscountValidationResultDto> ValidateForItemsAsync(ValidateDiscountDto dto)
        {
            var requested = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            var ids = requested.Select(r => r.ProductId).ToList();
            var prices = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Price);

            decimal subtotal = 0;
            foreach (var item in requested)
            {
                if (!prices.TryGetValue(item.ProductId, out var price))
                    throw new InvalidOperationException($"Product with ID {item.ProductId} does not exist.");

                subtotal += price * item.Quantity;
            }

            return await ValidateAsync(dto.Code, subtotal);
        }

        public async Task<IEnumerable<DiscountCodeDto>> GetAllAsync()
        {
            var codes = await _context.DiscountCodes
                .AsNoTracking()
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return codes.Select(ToDto).ToList();
        }

        public async Task<DiscountCodeDto?> GetByIdAsync(int id)
        {
            var code = await _context.DiscountCodes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            return code == null ? null : ToDto(code);
        }

        public async Task<DiscountCodeDto> CreateAsync(SaveDiscountCodeDto dto)
        {
            var code = Normalize(dto.Code);
            await EnsureValidAsync(dto, code, excludeId: null);

            var entity = new DiscountCode { CreatedAt = DateTime.UtcNow };
            Apply(entity, dto, code);

            _context.DiscountCodes.Add(entity);
            await SaveWithDuplicateCheckAsync(code);

            return ToDto(entity);
        }

        public async Task<DiscountCodeDto?> UpdateAsync(int id, SaveDiscountCodeDto dto)
        {
            var entity = await _context.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null)
                return null;

            var code = Normalize(dto.Code);
            await EnsureValidAsync(dto, code, excludeId: id);

            Apply(entity, dto, code);
            entity.UpdatedAt = DateTime.UtcNow;

            await SaveWithDuplicateCheckAsync(code);
            return ToDto(entity);
        }

        // Safe for past orders: they keep their own copy of the code text and discount amount
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null)
                return false;

            _context.DiscountCodes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureValidAsync(SaveDiscountCodeDto dto, string code, int? excludeId)
        {
            if (dto.Type == DiscountType.Percentage && dto.Value > 100)
                throw new InvalidOperationException("A percentage discount cannot be more than 100%.");

            var duplicate = await _context.DiscountCodes
                .AnyAsync(d => d.Code == code && (excludeId == null || d.Id != excludeId));
            if (duplicate)
                throw new InvalidOperationException($"The code '{code}' already exists.");
        }

        // The unique index catches two admins saving the same code at once
        private async Task SaveWithDuplicateCheckAsync(string code)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException($"The code '{code}' already exists.");
            }
        }

        private static void Apply(DiscountCode entity, SaveDiscountCodeDto dto, string code)
        {
            entity.Code = code;
            entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            entity.Type = dto.Type;
            entity.Value = Math.Round(dto.Value, 2, MidpointRounding.AwayFromZero);
            entity.MinOrderAmount = dto.MinOrderAmount.HasValue && dto.MinOrderAmount.Value > 0
                ? Math.Round(dto.MinOrderAmount.Value, 2, MidpointRounding.AwayFromZero)
                : null;
            entity.MaxUses = dto.MaxUses;
            entity.ExpiresAt = dto.ExpiresAt.HasValue ? ToUtc(dto.ExpiresAt.Value) : null;
            entity.IsActive = dto.IsActive;
        }

        // Dates without a zone are treated as UTC so comparisons with DateTime.UtcNow are correct
        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        private static DiscountCodeDto ToDto(DiscountCode d) => new()
        {
            Id = d.Id,
            Code = d.Code,
            Description = d.Description,
            Type = d.Type,
            TypeName = d.Type.ToString(),
            Value = d.Value,
            MinOrderAmount = d.MinOrderAmount,
            MaxUses = d.MaxUses,
            UsedCount = d.UsedCount,
            ExpiresAt = d.ExpiresAt,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            IsUsable = d.IsActive
                && (!d.ExpiresAt.HasValue || d.ExpiresAt.Value > DateTime.UtcNow)
                && (!d.MaxUses.HasValue || d.UsedCount < d.MaxUses.Value)
        };

        // Codes are stored upper case without surrounding spaces
        public static string Normalize(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

        // Rounded to cents, and never more than the subtotal (so the total can't go negative)
        public static decimal CalculateDiscount(DiscountCode discount, decimal subtotal)
        {
            if (subtotal <= 0)
                return 0;

            var raw = discount.Type == DiscountType.Percentage
                ? subtotal * discount.Value / 100m
                : discount.Value;

            var rounded = Math.Round(raw, 2, MidpointRounding.AwayFromZero);
            return Math.Min(rounded, subtotal);
        }

        private static DiscountValidationResultDto Invalid(string message, decimal subtotal) => new()
        {
            IsValid = false,
            Message = message,
            Subtotal = subtotal,
            DiscountAmount = 0,
            Total = subtotal
        };
    }
}
