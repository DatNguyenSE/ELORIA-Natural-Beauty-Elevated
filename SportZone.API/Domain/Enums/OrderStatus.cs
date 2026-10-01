namespace SportZone.Domain.Enums;
public enum OrderStatus
{
    Pending = 1,    // Đang chờ xử lý
    Placed = 2,     // Chức năng đặt trước
    Paid = 3,       // Đã thanh toán
    Shipped = 4,    // Đang giao
    Completed = 5,  // Hoàn thành
    Cancelled = 6   // Đã hủy
}