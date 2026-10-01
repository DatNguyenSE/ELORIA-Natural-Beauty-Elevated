using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Sport.Application.IService;
using SportZone.Application.Dtos.Vnpay;
using SportZone.Infrastructure.Common.Utilities; // Đã có VnPayLibrary ở đây

namespace Sport.Infrastructure.Service
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string? returnUrlOverride = null)
        {
            // Khởi tạo thư viện
            var vnpay = new VnPayLibrary();

            // 1. Lấy Config
            var timeZoneId = _configuration["TimeZoneId"] ?? "SE Asia Standard Time";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            
            var baseUrl = _configuration["VnPay:BaseUrl"];
            var hashSecret = _configuration["VnPay:HashSecret"];
            var returnUrl = !string.IsNullOrEmpty(returnUrlOverride) 
                            ? returnUrlOverride 
                            : _configuration["VnPay:ReturnUrl"];

            // 2. Add Request Data
            vnpay.AddRequestData("vnp_Version", _configuration["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _configuration["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _configuration["VnPay:TmnCode"]);
            
            // Số tiền nhân 100
            vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString()); 
            
            vnpay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _configuration["VnPay:CurrCode"]);
            
            vnpay.AddRequestData("vnp_IpAddr", vnpay.GetIpAddress(context));

            vnpay.AddRequestData("vnp_Locale", _configuration["VnPay:Locale"]);
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", model.OrderType ?? "other"); 
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            
            // Mã tham chiếu txnRef phải là duy nhất cho mỗi lần giao dịch , nếu trùng vnpay sẽ chặn nếu trước đó giao dịch sai 
            var txnRef = $"{model.OrderId}_{DateTime.Now.Ticks}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef); // 12_639045166745147581
        


            // Tạo URL
            var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);
            return paymentUrl;
        }

        public PaymentResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            
            var response = vnpay.GetFullResponseData(collections, _configuration["VnPay:HashSecret"]);
        
            return response;
        }
    }
}