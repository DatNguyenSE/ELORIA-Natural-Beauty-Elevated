namespace SportZone.Application.Dtos;

public class CartDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = new();
}
public class CartItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int ProductSizeId { get; set; }
     public string? SizeName { get; set; }
    public ProductInCartDto? Product { get; set; }
}

public class AddCartItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? SizeName { get; set; }
}

public class UpdateCartItemDto : AddCartItemDto
{

}




