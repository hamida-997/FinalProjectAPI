using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Helpers.DTOs.Discount
{
    public class DiscountCodeDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // True when the code would currently be accepted (ignoring minimum order)
        public bool IsUsable { get; set; }
    }
}
