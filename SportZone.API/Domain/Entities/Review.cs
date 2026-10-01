namespace SportZone.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int Rating { get; set; }

        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }

}
