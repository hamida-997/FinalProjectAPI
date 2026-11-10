using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Entities
{
    public class Order : BaseEntity
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        // Sum of order items before any discount
        public decimal Subtotal { get; set; }

        // Discount applied to this order (0 when no code was used)
        public decimal DiscountAmount { get; set; }

        // The code as entered, kept even if the code is later edited or deleted
        public string? DiscountCode { get; set; }

        // Amount charged: Subtotal - DiscountAmount
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }

        // Account that placed the order (null for orders created before checkout existed)
        public string? UserId { get; set; }

        // Payment
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public string? StripeSessionId { get; set; }
        public DateTime? PaidAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
