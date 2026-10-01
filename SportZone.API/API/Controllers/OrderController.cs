using SportZone.API.Extensions;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SportZone.API.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderController(IOrderService orderService) : ControllerBase
    {
        [HttpPost("add")]
        public async Task<IActionResult> CreateOrder(string? CouponCode,PaymentMethod paymentMethod)
        {
            var userId = User.GetUserId();
            var order = await orderService.CreateOrderByCartItemsAsync(userId,CouponCode, paymentMethod);
            return CreatedAtAction(nameof(GetOrderWithDetails), new { orderId = order.Id }, order);
        }

        [HttpGet("user-orders")]
        public async Task<IActionResult> GetOrdersByUserId()
        {
            var userId = User.GetUserId();
            var orders = await orderService.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{orderId:int}/details")]
        public async Task<IActionResult> GetOrderWithDetails(int orderId)
        {
            var userId = User.GetUserId();
            var order = await orderService.GetOrderWithDetailsAsync(orderId, userId);
            return Ok(order);
        }

        [HttpDelete("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = User.GetUserId();
            await orderService.CancelOrderAsync(orderId, userId);
            return Ok(new { Message = "Order cancelled successfully." });
        }

        [HttpGet("payment-status")]
        public async Task<ActionResult> GetOrderWithPaymentStatus(PaymentStatus paymentStatus)
        {
            var userId = User.GetUserId();
            var orders = await orderService.GetListOrderWithPaymentAsync(userId, paymentStatus);
            return Ok(orders);
        }
    }
}
