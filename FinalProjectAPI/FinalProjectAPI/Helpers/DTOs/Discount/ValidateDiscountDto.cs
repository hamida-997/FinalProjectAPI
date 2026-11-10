using System.ComponentModel.DataAnnotations;
using FinalProjectAPI.Helpers.DTOs.OrderItem;

namespace FinalProjectAPI.Helpers.DTOs.Discount
{
    // Checkout preview: the server prices the items itself, so the subtotal can't be faked
    public class ValidateDiscountDto
    {
        [Required(ErrorMessage = "Please enter a discount code.")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "Your cart is empty.")]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}
