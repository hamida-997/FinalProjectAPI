using FinalProjectAPI.Helpers.DTOs.Discount;

namespace FinalProjectAPI.Services
{
    public interface IDiscountService
    {
        /// <summary>
        /// Checks whether a code can be used for an order with this subtotal and,
        /// if so, how much it takes off. Never throws for invalid codes; check IsValid.
        /// </summary>
        Task<DiscountValidationResultDto> ValidateAsync(string? code, decimal subtotal);
    }
}
