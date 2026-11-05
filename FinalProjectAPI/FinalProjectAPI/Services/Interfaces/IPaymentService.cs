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

        /// <summary>
        /// Checks the session with Stripe and, if it is paid, marks the order Paid and
        /// decrements stock. Safe to call more than once for the same session.
        /// Returns the order ID. Throws InvalidOperationException if the payment isn't complete.
        /// </summary>
        Task<int> ConfirmPaymentAsync(string sessionId);
    }

    public record CheckoutSessionResult(int OrderId, string SessionId, string Url);
}
