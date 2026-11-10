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
