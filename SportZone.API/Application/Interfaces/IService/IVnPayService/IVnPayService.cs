using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos.Vnpay;
namespace Sport.Application.IService;
public interface IVnPayService
{
    string CreatePaymentUrl(PaymentInformationModel model, HttpContext context, string txnRef);
    PaymentResponseModel PaymentExecute(IQueryCollection collections);
}