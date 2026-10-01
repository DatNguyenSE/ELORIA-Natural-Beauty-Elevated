namespace SportZone.Domain.Entities
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!; // Mã coupon (VD: "SALE50")
        
        // --- Thông tin tính toán ---
        public decimal DiscountValue { get; set; } 
        public string DiscountType { get; set; } = "PERCENT"; 
        public decimal? MinOrderValue { get; set; } // Đơn tối thiểu được áp dụng
        public decimal? MaxDiscountAmount { get; set; } // Giảm tối đa (cho loại %)

        // --- Thông tin thời hạn ---
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        // --- Thông tin hiển thị cho Banner (Interface PromoBanner) ---
        public string? Title { get; set; }      
        public string? Description { get; set; } 
        public string? Icon { get; set; }       
        public bool IsVisible { get; set; } = true; 
    }
}