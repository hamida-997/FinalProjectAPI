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
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(AppDbContext context, IOptions<StripeSettings> settings, ILogger<PaymentService> logger)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;

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

            // Show the discount on Stripe's page with a single-use coupon for this order's exact
            // amount. Stripe's total then equals order.TotalAmount, which Confirm checks.
            if (order.DiscountAmount > 0)
            {
                var couponName = string.IsNullOrEmpty(order.DiscountCode) ? "Discount" : order.DiscountCode;
                var coupon = await new CouponService(_stripeClient).CreateAsync(new CouponCreateOptions
                {
                    AmountOff = ToSmallestUnit(order.DiscountAmount),
                    Currency = _settings.Currency,
                    Duration = "once",
                    MaxRedemptions = 1,
                    Name = couponName.Length > 40 ? couponName[..40] : couponName,
                    Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
                });

                options.Discounts = new List<SessionDiscountOptions>
                {
                    new() { Coupon = coupon.Id }
                };
            }

            var sessionService = new SessionService(_stripeClient);
            var session = await sessionService.CreateAsync(options);

            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();

            return new CheckoutSessionResult(order.Id, session.Id, session.Url);
        }

        public async Task<int> ConfirmPaymentAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new InvalidOperationException("Session ID is required.");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId)
                ?? throw new InvalidOperationException("No order found for this payment session.");

            // Already handled (by an earlier redirect or the webhook)
            if (order.PaymentStatus == PaymentStatus.Paid)
                return order.Id;

            // Never trust the redirect alone: ask Stripe whether the session is paid
            var session = await new SessionService(_stripeClient).GetAsync(sessionId);

            // "no_payment_required" happens when a 100% discount makes the order free
            var isPaid = session.PaymentStatus == "paid"
                || (session.PaymentStatus == "no_payment_required" && order.TotalAmount == 0
                    && session.Status == "complete");

            if (!isPaid)
                throw new InvalidOperationException("Payment has not been completed.");

            if (session.AmountTotal != ToSmallestUnit(order.TotalAmount))
            {
                _logger.LogError(
                    "Stripe amount {StripeAmount} does not match order {OrderId} total {OrderTotal}",
                    session.AmountTotal, order.Id, order.TotalAmount);
                throw new InvalidOperationException("Payment amount does not match the order total.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Conditional update: only one concurrent caller can flip Unpaid -> Paid.
            // The others update 0 rows and stop, so stock is decremented exactly once.
            var updated = await _context.Orders
                .Where(o => o.Id == order.Id && o.PaymentStatus != PaymentStatus.Paid)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.PaymentStatus, PaymentStatus.Paid)
                    .SetProperty(o => o.PaidAt, DateTime.UtcNow)
                    .SetProperty(o => o.Status,
                        o => o.Status == OrderStatus.Pending ? OrderStatus.Confirmed : o.Status));

            if (updated == 0)
            {
                await transaction.RollbackAsync();
                return order.Id;
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                // Paid on a stale tab after the order was cancelled: record the payment,
                // keep stock unchanged, and leave the refund to an admin.
                _logger.LogWarning("Order {OrderId} was paid after being cancelled; refund needed", order.Id);
            }
            else
            {
                foreach (var item in order.OrderItems)
                {
                    var rows = await _context.Products
                        .Where(p => p.Id == item.ProductId && p.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(
                            p => p.StockQuantity, p => p.StockQuantity - item.Quantity));

                    if (rows == 0)
                    {
                        // Stock ran out between checkout and payment. The customer has
                        // already paid, so keep the order and flag it for an admin.
                        _logger.LogWarning(
                            "Product {ProductId} oversold on order {OrderId}: requested {Quantity}",
                            item.ProductId, order.Id, item.Quantity);

                        await _context.Products
                            .Where(p => p.Id == item.ProductId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, 0));
                    }
                }

                // Count the code use only now that the order is paid. The condition keeps
                // UsedCount from going over MaxUses even when payments confirm at the same time.
                if (!string.IsNullOrEmpty(order.DiscountCode))
                {
                    var counted = await _context.DiscountCodes
                        .Where(d => d.Code == order.DiscountCode
                            && (d.MaxUses == null || d.UsedCount < d.MaxUses))
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.UsedCount, d => d.UsedCount + 1));

                    if (counted == 0)
                    {
                        // Limit reached (or code deleted) between checkout and payment.
                        // The customer already paid the discounted price, so honor it.
                        _logger.LogWarning(
                            "Discount code {Code} on order {OrderId} could not be counted (limit reached or code removed)",
                            order.DiscountCode, order.Id);
                    }
                }
            }

            await transaction.CommitAsync();
            return order.Id;
        }

        // Stripe expects amounts in the smallest currency unit (cents for USD/EUR).
        // Zero-decimal currencies like JPY would need different handling.
        private static long ToSmallestUnit(decimal amount)
        {
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }
    }
}
