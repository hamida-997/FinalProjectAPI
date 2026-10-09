using System.ComponentModel.DataAnnotations;
using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Helpers.DTOs.Order
{
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Order status is required.")]
        public OrderStatus Status { get; set; }

        public DateTime? DeliveryDate { get; set; }
    }
}
