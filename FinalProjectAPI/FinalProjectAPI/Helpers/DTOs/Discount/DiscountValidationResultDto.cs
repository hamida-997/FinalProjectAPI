namespace FinalProjectAPI.Helpers.DTOs.Discount
{
    // Result of checking a code against an order subtotal
    public class DiscountValidationResultDto
    {
        public bool IsValid { get; set; }

        // User-facing reason when IsValid is false
        public string? Message { get; set; }

        // Normalized code (upper case) when valid
        public string? Code { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
    }
}
