using FinalProjectAPI.Helpers.DTOs.Auth;

namespace FinalProjectAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        // Verifies the link from the confirmation email
        Task<AuthResponseDto> ConfirmEmailAsync(string userId, string token);

        // Sends a new link if the account exists and isn't confirmed. Never reveals whether it exists.
        Task ResendConfirmationAsync(string email);
    }
}
