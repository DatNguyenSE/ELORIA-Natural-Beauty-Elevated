using System;
using SportZone.Application.Dtos;
using SportZone.Domain.Enums;


namespace SportZone.Application.Interfaces.IService;

public interface IOrderService
{
    Task<IEnumerable<OrderDetailsDto>> GetListOrderWithPaymentAsync(string userId, PaymentStatus paymentStatus);
    Task<OrderDetailsDto?> GetOrderWithDetailsAsync(int orderId, string userId);
    Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId);
    Task<OrderDetailsDto> CreateOrderByCartItemsAsync(string userId,string? couponCode, PaymentMethod paymentMethod);
    Task CancelOrderAsync(int orderId, string userId);
    Task UpdateOrderStatus(int orderId, OrderStatus orderStatus);
    Task<OrderDto> GetOrderByIdAsync(int orderId);
    Task CompletedOrderStatus(int orderId, string userId);
    Task<OrderDetailsDto> GetOrderWithPaymentAsync(int orderId);
    Task CancelOrderBackgroundAsync(int orderId);
}
