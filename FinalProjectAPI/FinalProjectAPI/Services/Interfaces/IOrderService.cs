using FinalProjectAPI.Helpers.DTOs.Order;
using FinalProjectAPI.Helpers.Enums;

namespace FinalProjectAPI.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status);
        Task<IEnumerable<OrderDto>> GetOrdersByCustomerEmailAsync(string email);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        /// <summary>
        /// Creates an order priced from the database. Throws InvalidOperationException
        /// with a user-facing message if a product is missing, unavailable or out of stock.
        /// </summary>
        Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto, string? userId);
        Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto);
        Task<bool> CancelOrderAsync(int id);
        Task<bool> OrderExistsAsync(int id);
    }
}
