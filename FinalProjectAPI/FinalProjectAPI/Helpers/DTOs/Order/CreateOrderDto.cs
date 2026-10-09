using System.ComponentModel.DataAnnotations;
using FinalProjectAPI.Helpers.DTOs.OrderItem;

namespace FinalProjectAPI.Helpers.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer email is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer phone is required.")]
        [Phone(ErrorMessage = "Please provide a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required.")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Shipping address must be between 10 and 500 characters.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "At least one order item is required.")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }
}
