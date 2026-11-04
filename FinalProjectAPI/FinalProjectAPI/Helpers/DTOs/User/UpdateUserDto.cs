using System.ComponentModel.DataAnnotations;

namespace FinalProjectAPI.Helpers.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
