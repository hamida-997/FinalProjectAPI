namespace FinalProjectAPI.DTOs
{
    public class ProductImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
