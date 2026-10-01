namespace SportZone.Application.Dtos.Vnpay
{
    public class PaymentRequestDto
    {
        public required int OrderId { get; set; } 
        public string? Description { get; set; }
    }
}