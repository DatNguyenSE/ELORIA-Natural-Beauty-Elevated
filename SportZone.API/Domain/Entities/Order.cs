using SportZone.Domain.Enums;

namespace SportZone.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public decimal SubTotal { get; set; }      // Tổng tiền hàng (chưa trừ gì)
        public decimal DiscountAmount { get; set; } // Số tiền được giảm
        public string? CouponCode { get; set; }     // Lưu mã coupon đã dùng (để đối soát)
        public decimal TotalAmount { get; set; } // Số tiền cuối cùng khách phải trả (SubTotal - Discount)

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }
    }

}
