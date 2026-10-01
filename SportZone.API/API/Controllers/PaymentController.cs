using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sport.Application.IService;
using SportZone.API.Extensions;
using SportZone.Application.Dtos.Vnpay;
using SportZone.Application.Interfaces.IService;
using SportZone.Application.Services;
using SportZone.Domain.Enums;

namespace SportZone.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IOrderService _orderService;
        public PaymentController(IVnPayService vnPayService, IOrderService orderService)
        {
            _vnPayService = vnPayService;
            _orderService = orderService;
        }

        [HttpPost("create-payment-url")]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] PaymentRequestDto request)
        {
            var order = await _orderService.GetOrderWithPaymentAsync(request.OrderId);

            if (order == null) return NotFound("Your order not found!");
            if (order.Status == OrderStatus.Paid.ToString()
                || order.Status == OrderStatus.Cancelled.ToString()
                || order.Status == OrderStatus.Completed.ToString())
                return BadRequest($"Unable to process payment. The order is currently '{order.Status}'.");
            if (order.Payment.PaymentMethod == PaymentMethod.COD.ToString())
            {
                return BadRequest("Payment cannot be processed. The order is currently in the COD payment stage.");
            }
            var paymentModel = new PaymentInformationModel()
            {
                OrderId = order.Id.ToString(),
                Amount = (double)order.TotalAmount,
                OrderDescription = $"Pay for order - {order.Id}"

            };
            var returnUrl = "https://nbhaeggp4n.ap-southeast-2.awsapprunner.com/api/payments/callback";
            var url = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext, returnUrl);
            return Ok(new { url });
        }

        [HttpGet("callback")]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (!response.Success || response.VnPayResponseCode != "00")
            {
                // Làm sạch ID trước khi gửi
                string cleanId = response.OrderId.Contains("_") ? response.OrderId.Split('_')[0] : response.OrderId;

                // Đổi từ ?orderId= sang / (Path Parameter)
                return Redirect($"https://nbhaeggp4n.ap-southeast-2.awsapprunner.com/payment-fail/{cleanId}");
            }

            // --- XỬ LÝ CẮT CHUỖI "12_ticks" ---
            string vnpTxnRef = response.OrderId; // Ví dụ: "12_639045166745147581"
            string orderIdRaw = vnpTxnRef;

            if (vnpTxnRef.Contains("_"))
            {
                orderIdRaw = vnpTxnRef.Split('_')[0]; // Lấy "12"
            }

            if (!int.TryParse(orderIdRaw, out int orderId))
            {
                return BadRequest("Mã đơn hàng lỗi định dạng");
            }
            // --- output là orderId:int ---

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                 return Redirect($"https://nbhaeggp4n.ap-southeast-2.awsapprunner.com/payment-fail/{orderId}");

            long vnpayAmount = response.Amount;
            long orderAmount = (long)(order.TotalAmount * 100); // vì vnpay trả amount về đã nhân với 100 nên ta nhân với order.amount để sosanh

            if (vnpayAmount != orderAmount) return BadRequest("The payment amounts don't match!");

            string userId = order.UserId;
            if (order.Status != OrderStatus.Paid.ToString())
            {

                await _orderService.CompletedOrderStatus(orderId, userId);
            }
            // Redirect về trang thành công 
            // return Redirect($"/order-detail?orderId={response.OrderId}");
            return Redirect($"https://nbhaeggp4n.ap-southeast-2.awsapprunner.com/checkout-success/{orderId}");
        }
    }
}