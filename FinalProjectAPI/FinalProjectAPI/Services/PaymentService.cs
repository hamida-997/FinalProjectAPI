using FinalProjectAPI.Data;
using FinalProjectAPI.Helpers.Enums;
using FinalProjectAPI.Helpers.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FinalProjectAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly StripeSettings _settings;
        private readonly IStripeClient _stripeClient;

        public PaymentService(AppDbContext context, IOptions<StripeSettings> settings)
        {
            _context = context;
            _settings = settings.Value;

            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe secret key is not configured. Set StripeSettings:SecretKey with dotnet user-secrets.");
            }

            _stripeClient = new StripeClient(_settings.SecretKey);
        }

        public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException($"Order with ID {orderId} not found.");

            if (order.PaymentStatus == PaymentStatus.Paid)
                throw new InvalidOperationException("This order has already been paid.");

            if (order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("This order has been cancelled.");

            if (order.OrderItems.Count == 0)
                throw new InvalidOperationException("This order has no items.");

            // Line items use the prices stored on the order, which were taken
            // from the database when the order was created.
            var lineItems = order.OrderItems.Select(item => new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = _settings.Currency,
                    UnitAmount = ToSmallestUnit(item.UnitPrice),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName
                    }
                }
            }).ToList();

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                LineItems = lineItems,
                CustomerEmail = order.CustomerEmail,
                ClientReferenceId = order.Id.ToString(),
                SuccessUrl = _settings.SuccessUrl,
                CancelUrl = _settings.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.Id.ToString()
                }
            };

            var sessionService = new SessionService(_stripeClient);
            var session = await sessionService.CreateAsync(options);

            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();

            return new CheckoutSessionResult(order.Id, session.Id, session.Url);
        }

        // Stripe expects amounts in the smallest currency unit (cents for USD/EUR).
        // Zero-decimal currencies like JPY would need different handling.
        private static long ToSmallestUnit(decimal amount)
        {
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }
    }
}
