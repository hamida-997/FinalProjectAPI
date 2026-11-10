namespace FinalProjectAPI.Entities
{
    public class Review : BaseEntity
    {
        public int ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;

        // 1 to 5 stars (enforced by a database check constraint)
        public int Rating { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public AppUser User { get; set; } = null!;
    }
}
