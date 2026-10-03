namespace FinalProjectAPI.DTOs
{
    public class CreateProductImageDto
    {
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public int ProductId { get; set; }
    }
}
