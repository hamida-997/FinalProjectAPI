namespace FinalProjectAPI.DTOs
{
    public class UpdateProductImageDto
    {
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
