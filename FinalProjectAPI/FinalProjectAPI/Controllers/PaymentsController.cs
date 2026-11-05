using System.Security.Claims;
using FinalProjectAPI.Helpers.DTOs.Order;
using FinalProjectAPI.Helpers.DTOs.Payment;
using FinalProjectAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace FinalProjectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IOrderService orderService,
            IPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _logger = logger;
        }

        // POST: api/Payments/checkout
        // Creates an unpaid order for the logged-in user and returns the Stripe payment URL.
        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutSessionResult>> Checkout(CreateOrderDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "You must be logged in to check out." });
            }

            OrderDto order;
            try
            {
                order = await _orderService.CreateOrderAsync(createDto, userId);
            }
            catch (InvalidOperationException ex)
            {
                // Validation problems: missing product, unavailable, out of stock
                return BadRequest(new { message = ex.Message });
            }

            try
            {
                var session = await _paymentService.CreateCheckoutSessionAsync(order.Id);
                return Ok(session);
            }
            catch (Exception ex) when (ex is StripeException or InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to create Stripe checkout session for order {OrderId}", order.Id);

                // Don't leave an order behind that can never be paid
                await _orderService.CancelOrderAsync(order.Id);

                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Payment could not be started. Please try again later." });
            }
        }

        // POST: api/Payments/confirm
        // Called by the MVC success page after Stripe redirects back.
        [HttpPost("confirm")]
        public async Task<ActionResult<OrderDto>> Confirm(ConfirmPaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int orderId;
            try
            {
                orderId = await _paymentService.ConfirmPaymentAsync(dto.SessionId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Failed to verify Stripe session {SessionId}", dto.SessionId);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Payment could not be verified. Please try again later." });
            }

            var order = await _orderService.GetOrderByIdAsync(orderId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

            if (order == null || (!isAdmin && order.UserId != userId))
            {
                return NotFound(new { message = "Order not found." });
            }

            return Ok(order);
        }
    }
}
