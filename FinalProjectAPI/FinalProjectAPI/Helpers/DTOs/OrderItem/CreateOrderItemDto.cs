using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.OrderItem
{
    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "Product is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int Quantity { get; set; }
    }
}
