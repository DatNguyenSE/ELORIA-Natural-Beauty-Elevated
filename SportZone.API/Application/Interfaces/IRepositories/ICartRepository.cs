using System;
using SportZone.Domain.Entities;
namespace SportZone.Application.Interfaces.IRepositories;

public interface ICartRepository : IGenericRepository<Cart>
{
    Task<Cart?> GetCartByUserIdAsync(string userId);
    Task<bool> AddItemToCartAsync(string userId, int productId, int quantity, int ProductSizeId);
    Task<bool> RemoveItemFromCartAsync(string userId, int productId, string? sizeName = null);
    Task<bool> UpdateItemQuantityAsync(string userId, int productId, int quantity, string? sizeName = null);
    Task<bool> ClearCartAsync(string userId);
    Task<int> GetItemQuantityInCartAsync(string userId, int productId, string? sizeName = null);
    Task<IEnumerable<CartItem>> GetCartItemsBySizeIdsAsync(List<int> sizeIds);
    void RemoveCartItem(CartItem item); 

    
}
