
namespace SportZone.Domain.Entities
{
    public class ProductSize
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Lưu cứng Size và Color dạng chuỗi (String)
        public string SizeName { get; set; } = null!;

        public int Quantity { get; set; }

        public bool IsActive { get; set; } = true;

    }
}