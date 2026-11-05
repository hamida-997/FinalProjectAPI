using System.Security.Claims;
using FinalProjectAPI.Helpers.DTOs.Order;
using FinalProjectAPI.Helpers.Enums;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;

        public OrdersController(IOrderService orderService, IProductService productService)
        {
            _orderService = orderService;
            _productService = productService;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            // Return 404 (not 403) for other people's orders so IDs can't be probed
            if (order == null || !CanAccess(order))
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            return Ok(order);
        }

        [HttpGet("Status/{status}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByStatus(OrderStatus status)
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }

        [HttpGet("Customer/{email}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomerEmail(string email)
        {
            if (!IsAdmin && !string.Equals(email, CurrentEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var orders = await _orderService.GetOrdersByCustomerEmailAsync(email);
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var order = await _orderService.CreateOrderAsync(createDto, userId);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/Status")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, UpdateOrderStatusDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = await _orderService.UpdateOrderStatusAsync(id, updateDto);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            return Ok(order);
        }

        [HttpPut("{id}/Cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null || !CanAccess(order))
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            var result = await _orderService.CancelOrderAsync(id);

            if (!result)
            {
                return BadRequest(new { message = $"Order with ID {id} cannot be cancelled. It may be already delivered or cancelled." });
            }

            return Ok(new { message = "Order cancelled successfully." });
        }

        private bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // The JWT "email" claim may or may not be mapped to ClaimTypes.Email, so check both
        private string? CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

        private bool CanAccess(OrderDto order)
        {
            if (IsAdmin) return true;

            // Orders placed through checkout are linked to the account
            if (!string.IsNullOrEmpty(order.UserId))
                return order.UserId == CurrentUserId;

            // Older orders have no UserId; fall back to matching the email
            return !string.IsNullOrEmpty(CurrentEmail)
                && string.Equals(order.CustomerEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
