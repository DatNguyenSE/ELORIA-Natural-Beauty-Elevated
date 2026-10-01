using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Enums;
using SportZone.Domain.Exceptions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using SportZone.Domain.Entities;
using Hangfire;

namespace SportZone.Application.Services;

public class OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderDetailsDto> CreateOrderByCartItemsAsync(string userId, string? couponCode, PaymentMethod paymentMethod)
    {
        // 1. Get user cart
        var userCart = await uow.CartRepository.GetCartByUserIdAsync(userId);

        if (userCart == null || userCart.Items.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty or does not exist.");
        }

        // 2. TỐI ƯU: Load Inventory và chuyển sang Dictionary để tra cứu nhanh O(1)
        var productIds = userCart.Items.Select(i => i.ProductId).Distinct().ToList();
        var productSizeIds = userCart.Items.Select(i => i.ProductSizeId).Distinct().ToList();

        var productSizes = await uow.ProductSizeRepository.GetListByProductIdsAsync(productIds, productSizeIds);

        // Key là ProductId, Value là Inventory Object
        var productSizeDict = productSizes.ToDictionary(x => (x.ProductId, x.Id), x => x);

        // 3. Create order object
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>()
        };

        decimal subTotal = 0;

        foreach (var cartItem in userCart.Items)
        {
            // Validate Product exists in cart
            if (cartItem.Product == null)
                throw new Exception($"Product info missing for CartItem ID: {cartItem.CartId}");

            productSizeDict.TryGetValue((cartItem.ProductId, cartItem.ProductSizeId), out var productSize);

            // Check stock
            var availableStock = productSize != null ? productSize.Quantity : cartItem.Product.Stock;
            if (availableStock < cartItem.Quantity)
            {
                throw new InvalidOperationException($"Sản phẩm '{cartItem.Product.Name}' không đủ số lượng trong kho (Còn: {availableStock}, Yêu cầu: {cartItem.Quantity}).");
            }

            // Update stock (Memory)
            if (productSize != null)
            {
                productSize.Quantity -= cartItem.Quantity;
            }
            if (cartItem.Product.Stock >= cartItem.Quantity)
            {
                cartItem.Product.Stock -= cartItem.Quantity;
            }

            decimal discountPercent = (decimal)(cartItem.Product.Discount ?? 0);

            decimal unitPrice = discountPercent > 0
                ? cartItem.Product.Price * (1 - discountPercent / 100)
                : cartItem.Product.Price;

            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice,
                ProductSizeId = cartItem.ProductSizeId,
                SizeName = productSize?.SizeName ?? cartItem.Product.Volume ?? "Tiêu chuẩn"
            };

            subTotal += orderItem.Quantity * orderItem.UnitPrice;
            order.Items.Add(orderItem);
        }
        // 5. XỬ LÝ KHUYẾN MÃI (PROMOTION)
        decimal discountAmount = 0;

        if (!string.IsNullOrEmpty(couponCode))
        {
            // Cần thêm PromotionRepository vào UnitOfWork
            var promotion = await uow.PromotionRepository.GetByCodeAsync(couponCode);

            // Validate cơ bản
            if (promotion != null && promotion.IsActive)
            {
                var now = DateTime.UtcNow;
                if (promotion.StartDate <= now && promotion.EndDate >= now)
                {
                    // Check đơn tối thiểu
                    if (promotion.MinOrderValue == null || subTotal >= promotion.MinOrderValue)
                    {
                        // Tính toán giảm giá
                        if (promotion.DiscountType == "FIXED")
                        {
                            discountAmount = promotion.DiscountValue;
                        }
                        else if (promotion.DiscountType == "PERCENT")
                        {
                            discountAmount = subTotal * (promotion.DiscountValue / 100);

                            // Check giảm tối đa
                            if (promotion.MaxDiscountAmount.HasValue && discountAmount > promotion.MaxDiscountAmount.Value)
                            {
                                discountAmount = promotion.MaxDiscountAmount.Value;
                            }
                        }
                    }
                }
            }
        }

        // Đảm bảo không giảm quá số tiền hàng (tránh âm tiền)
        if (discountAmount > subTotal) discountAmount = subTotal;

        // Gán các giá trị tiền tệ vào Order
        order.SubTotal = subTotal;
        order.DiscountAmount = discountAmount;
        order.CouponCode = discountAmount > 0 ? couponCode : null; // Chỉ lưu mã nếu áp dụng thành công
        order.TotalAmount = subTotal - discountAmount;


        // 6. Payment Logic
        if (paymentMethod == PaymentMethod.COD)
        {
            order.Payment = new Payment
            {
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                Amount = order.TotalAmount, // Lưu số tiền cần thu
                PaidAt = null
            };
            order.Status = OrderStatus.Placed;
        }
        else if (paymentMethod == PaymentMethod.OnlineBanking)
        {
            order.Payment = new Payment
            {
                PaymentMethod = PaymentMethod.OnlineBanking,
                PaymentStatus = PaymentStatus.Pending,
                Amount = order.TotalAmount, // Lưu số tiền cần thanh toán online
                PaidAt = null
            };
            order.Status = OrderStatus.Pending;
        }
        else
        {
            throw new BadRequestException($"Payment method '{paymentMethod}' is not supported.");
        }

        // 7. Save changes
        await uow.OrderRepository.AddAsync(order);
        await uow.CartRepository.ClearCartAsync(userId);
        await uow.Complete();

        // Lên lịch: Sau 15 phút sẽ gọi hàm CancelOrderBackgroundAsync
        if (paymentMethod == PaymentMethod.OnlineBanking)
        {
            BackgroundJob.Schedule<IOrderService>(
                service => service.CancelOrderBackgroundAsync(order.Id),
                TimeSpan.FromMinutes(10));
        }
        return mapper.Map<OrderDetailsDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId)
    {
        var orders = await uow.OrderRepository.GetOrdersByUserIdAsync(userId);
        return mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<OrderDetailsDto?> GetOrderWithDetailsAsync(int orderId, string userId)
    {
        var orderEntity = await uow.OrderRepository.GetOrderWithDetailsAsync(orderId, userId);
        if (orderEntity == null)
        {
            throw new NotFoundException($"Order with ID {orderId} not found.");
        }
        return mapper.Map<OrderDetailsDto>(orderEntity);
    }

    public async Task<IEnumerable<OrderDetailsDto>> GetListOrderWithPaymentAsync(string userId, PaymentStatus paymentStatus)
    {
        var orderEntity = await uow.OrderRepository.GetListOrderWithPaymentAsync(userId, paymentStatus);

        return mapper.Map<IEnumerable<OrderDetailsDto>>(orderEntity);
    }

    public async Task CancelOrderAsync(int orderId, string userId)
    {
        // Đảm bảo Repository có Include(Items)
        var order = await uow.OrderRepository.GetOrderWithDetailsAsync(orderId, userId)
            ?? throw new NotFoundException($"Order with ID {orderId} not found!!!");

        if (order.Payment?.PaymentStatus != null && order.Payment.PaymentStatus != PaymentStatus.Pending)
        {
            throw new BadRequestException($"Cannot cancel order. Payment status is {order.Payment.PaymentStatus}");
        }

        // Update status
        if (order.Payment != null)
            order.Payment.PaymentStatus = PaymentStatus.Failed;
        order.Status = OrderStatus.Cancelled;

        // REFUND ITEMS QUANTITY 
        await RefundStockAsync(order);


        await uow.Complete();
    }

    public async Task UpdateOrderStatus(int orderId, OrderStatus orderStatus)
    {
        var order = await uow.OrderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new BadRequestException("Order not found");
        }
        order.Status = orderStatus;

        await uow.Complete();
    }

    public async Task UpdateOrder(int orderId, OrderDetailsDto dto)
    {
        var order = uow.OrderRepository.GetByIdAsync(orderId);

    }

    public async Task CompletedOrderStatus(int orderId, string userId)
    {
        var order = await uow.OrderRepository.GetOrderWithPaymentAsync(orderId); //include Payment

        if (order == null)
        {
            throw new BadRequestException("Order not found");
        }
        
        if (order.Status != OrderStatus.Paid)
        {
            order.Status = OrderStatus.Paid;
            

            if (order.Payment != null)
            {
                order.Payment.PaymentStatus = PaymentStatus.Success;
                order.Payment.PaidAt = DateTime.UtcNow;
                await AddPoints(userId, order.TotalAmount);
               
            }
            else
            {
                // Trường hợp dữ liệu bị lỗi: Có đơn hàng nhưng chưa tạo record Payment
                // Bạn có thể tạo mới Payment tại đây hoặc log warning tùy nghiệp vụ
            }

            await uow.Complete();
        }
    }

    private async Task AddPoints(string userId, decimal totalPay)
    {
        int points = (int)Math.Floor(totalPay / 1000m);
         await uow.MembersRepository.AddPointsAsync(userId, points);
    }

    public async Task<OrderDto> GetOrderByIdAsync(int orderId)
    {
        var order = await uow.OrderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new NotFoundException("Order not found");
        }
        return mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDetailsDto> GetOrderWithPaymentAsync(int orderId)
    {
        var order = await uow.OrderRepository.GetOrderWithPaymentAsync(orderId);
        return mapper.Map<OrderDetailsDto>(order);
    }

    public async Task CancelOrderBackgroundAsync(int orderId)
    {
      
        var order = await uow.OrderRepository.GetOrderForBackgroundCancelAsync(orderId); //không check userId

        if (order != null && order.Status == OrderStatus.Pending)
        {
            logger.LogInformation($"Hangfire: Tự động hủy đơn hàng {orderId} do hết hạn thanh toán.");

            order.Status = OrderStatus.Cancelled;
            if (order.Payment != null) order.Payment.PaymentStatus = PaymentStatus.Failed;


            // REFUND ITEMS
            await RefundStockAsync(order);

            await uow.Complete();
        }
    }
    private async Task RefundStockAsync(Order order)
    {
        if (order.Items == null || !order.Items.Any()) return;

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var sizeIds = order.Items.Select(i => i.ProductSizeId).Distinct().ToList();

        var productSizes = await uow.ProductSizeRepository.GetListByProductIdsAsync(productIds, sizeIds);
        var productSizeDict = productSizes.ToDictionary(i => (i.ProductId, i.Id), i => i);

        foreach (var item in order.Items)
        {
            if (productSizeDict.TryGetValue((item.ProductId, item.ProductSizeId), out var productSize))
            {
                productSize.Quantity += item.Quantity;
            }
        }
    }
}
