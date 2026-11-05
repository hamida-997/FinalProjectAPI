namespace FinalProjectAPI.Helpers.Settings
{
    public class StripeSettings
    {
        public const string SectionName = "StripeSettings";

        // Secret values: set with `dotnet user-secrets`, never in appsettings.json
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;

        // Three-letter ISO currency code in lowercase, e.g. "usd", "eur"
        public string Currency { get; set; } = "usd";

        // MVC pages Stripe redirects to. {CHECKOUT_SESSION_ID} is filled in by Stripe.
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
