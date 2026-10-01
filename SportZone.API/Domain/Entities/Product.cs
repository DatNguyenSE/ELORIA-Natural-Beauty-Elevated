namespace SportZone.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsNew { get; set; } = true;
        public string? Label { get; set; } = null; // e.g., "New Arrival", "Best Seller", to set banners on UI
        public string? PublicId { get; set; } // Cloudinary public ID
        public double? Discount { get; set; } = 0.0;

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Hair care & cosmetics properties
        public string? Volume { get; set; } // e.g. "400ml", "300ml", "150ml"
        public int Stock { get; set; } = 0; // Total available stock
        public string ProductType { get; set; } = "single"; // "single" or "combo"
        public decimal? CompareAtPrice { get; set; } // Giá niêm yết gốc (để tính tiết kiệm combo)
        public string? Benefits { get; set; } // Công dụng chính
        public string? Usage { get; set; } // Hướng dẫn sử dụng
        public string? Ingredients { get; set; } // Thành phần chiết xuất
        public string? AccentColor { get; set; } // Màu đại diện sản phẩm (Hex/Tailwind)

        // Combo relationships
        public ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>(); // If this is a combo, its items
        public ICollection<ComboItem> PartOfCombos { get; set; } = new List<ComboItem>(); // If this is a component of combos

        // alter Inventory to ProductSize for multiple sizes and each size has its own quantity
        public ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Feature> Features { get; set; } = new List<Feature>();
    }
}