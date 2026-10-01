using SportZone.Domain.Enums;

namespace SportZone.Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }

}
