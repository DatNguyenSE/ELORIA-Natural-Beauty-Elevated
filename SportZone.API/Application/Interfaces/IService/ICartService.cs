using System;
using SportZone.Application.Dtos;

namespace SportZone.Application.Interfaces.IService;

public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(string userId);
    Task AddItemToCartAsync(string userId, int productId, int quantity, string? sizeName = null);
    Task RemoveItemFromCartAsync(string userId, int productId, string? sizeName = null);
    Task UpdateItemQuantityAsync(string userId, int productId, int quantity, string? sizeName = null);
    Task ClearCartAsync(string userId);
}
