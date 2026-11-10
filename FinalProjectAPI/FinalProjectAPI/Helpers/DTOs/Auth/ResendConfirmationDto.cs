using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.Auth
{
    public class ResendConfirmationDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}
