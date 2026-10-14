using AutoMapper;
using FinalProjectAPI.Data;
using FinalProjectAPI.Helpers.DTOs.Order;
using FinalProjectAPI.Helpers.Enums;
using FinalProjectAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public OrderService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(OrderStatus status)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerEmailAsync(string email)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order == null ? null : _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto)
        {
            var order = _mapper.Map<Order>(createDto);

            decimal totalAmount = 0;
            foreach (var itemDto in createDto.OrderItems)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    continue;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * itemDto.Quantity
                };

                order.OrderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;
            }

            order.TotalAmount = totalAmount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _context.Entry(order)
                .Collection(o => o.OrderItems)
                .Query()
                .Include(oi => oi.Product)
                .LoadAsync();

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return null;

            order.Status = updateDto.Status;
            order.DeliveryDate = updateDto.DeliveryDate;

            await _context.SaveChangesAsync();

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                return false;

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> OrderExistsAsync(int id)
        {
            return await _context.Orders.AnyAsync(o => o.Id == id);
        }
    }
}
