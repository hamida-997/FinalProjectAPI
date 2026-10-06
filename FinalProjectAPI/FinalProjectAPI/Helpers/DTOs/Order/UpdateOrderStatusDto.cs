using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Helpers.DTOs.Order
{
    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
}
