namespace SportZone.Domain.Entities
{
    public class OrderItem
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        
        public int ProductSizeId { get; set; }
        public ProductSize ProductSize { get; set; } = null!;

        public string SizeName { get; set; } = null!; //Snapshot 
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

}
