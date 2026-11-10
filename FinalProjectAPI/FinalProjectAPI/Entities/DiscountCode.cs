using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Entities
{
    public class DiscountCode : BaseEntity
    {
        // Stored in upper case, e.g. "SWEET10". Unique.
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DiscountType Type { get; set; } = DiscountType.Percentage;

        // Percentage (1-100) or fixed amount, depending on Type
        public decimal Value { get; set; }

        // Optional limits; null means "no limit"
        public decimal? MinOrderAmount { get; set; }
        public int? MaxUses { get; set; }
        public DateTime? ExpiresAt { get; set; }

        // Counted only when an order using the code is paid
        public int UsedCount { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
