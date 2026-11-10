using System.ComponentModel.DataAnnotations;
using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Helpers.DTOs.Discount
{
    // Used for both create and update. UsedCount is never set by the client.
    public class SaveDiscountCodeDto
    {
        [Required(ErrorMessage = "Code is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters.")]
        [RegularExpression(@"^\s*[A-Za-z0-9_-]+\s*$", ErrorMessage = "Code can only contain letters, numbers, - and _.")]
        public string Code { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string? Description { get; set; }

        [Required]
        [EnumDataType(typeof(DiscountType), ErrorMessage = "Invalid discount type.")]
        public DiscountType Type { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Value must be greater than 0.")]
        public decimal Value { get; set; }

        [Range(0, 100000, ErrorMessage = "Minimum order amount cannot be negative.")]
        public decimal? MinOrderAmount { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max uses must be at least 1.")]
        public int? MaxUses { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
