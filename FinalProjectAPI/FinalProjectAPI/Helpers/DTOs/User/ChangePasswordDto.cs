using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.User
{
    public class ChangePasswordDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
