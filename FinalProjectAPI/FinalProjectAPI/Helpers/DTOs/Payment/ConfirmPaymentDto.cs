using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.Payment
{
    public class ConfirmPaymentDto
    {
        [Required(ErrorMessage = "Session ID is required.")]
        [StringLength(255)]
        public string SessionId { get; set; } = string.Empty;
    }
}
