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

        /// <summary>
        /// Prices the items from the database, then validates the code against that subtotal.
        /// Throws InvalidOperationException if a product doesn't exist.
        /// </summary>
        Task<DiscountValidationResultDto> ValidateForItemsAsync(ValidateDiscountDto dto);

        // Admin management
        Task<IEnumerable<DiscountCodeDto>> GetAllAsync();
        Task<DiscountCodeDto?> GetByIdAsync(int id);

        // Throw InvalidOperationException with a user-facing message (duplicate code, percentage over 100)
        Task<DiscountCodeDto> CreateAsync(SaveDiscountCodeDto dto);
        Task<DiscountCodeDto?> UpdateAsync(int id, SaveDiscountCodeDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
