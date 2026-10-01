namespace SportZone.Domain.Entities
{
    public class CartItem
    {
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int ProductSizeId { get; set; }
        public ProductSize ProductSize { get; set; } = null!;

        public int Quantity { get; set; }
    }

}
