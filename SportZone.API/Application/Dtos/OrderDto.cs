using System.Text.Json.Serialization;

namespace SportZone.Application.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; } 
    
    public string Status { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderDetailsDto : OrderDto
{

    public decimal SubTotal { get; set; } 
    
    public decimal DiscountAmount { get; set; } 
    
    public string? CouponCode { get; set; }

    [JsonPropertyOrder(100)]
    public PaymentDto? Payment { get; set; }
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; } 

    public string SizeName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    
}

