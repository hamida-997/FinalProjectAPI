namespace FinalProjectAPI.Services
{
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a Stripe Checkout session for an existing unpaid order, saves the
        /// session ID on the order, and returns the URL to send the customer to.
        /// Throws InvalidOperationException if the order can't be paid.
        /// </summary>
        Task<CheckoutSessionResult> CreateCheckoutSessionAsync(int orderId);
    }

    public record CheckoutSessionResult(int OrderId, string SessionId, string Url);
}
