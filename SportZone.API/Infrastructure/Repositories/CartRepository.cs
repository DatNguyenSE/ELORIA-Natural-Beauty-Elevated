using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Repositories;

public class CartRepository(AppDbContext _context) : GenericRepository<Cart>(_context), ICartRepository
{
    public async Task<bool> AddItemToCartAsync(string userId, int productId, int quantity, int ProductSizeId)
    {
        var cart = await GetCartByUserIdAsync(userId); 
        if (cart == null)
        {
            var newCart = new Cart
            {
                UserId = userId,
                Items = new List<CartItem> { new() { ProductId = productId, Quantity = quantity, ProductSizeId = ProductSizeId } }
                
            };
            await _context.Carts.AddAsync(newCart);

            return true;
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.ProductSizeId == ProductSizeId);
        if (existingItem != null)
        {
             await _context.CartItems
                 .Where(i => i.CartId == cart.Id && i.ProductId == productId && i.ProductSizeId == ProductSizeId)
                 .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => i.Quantity + quantity));
        }
        else                    
        {
            var newItem = new CartItem 
            { 
                CartId = cart.Id, 
                ProductId = productId, 
                Quantity = quantity,
                ProductSizeId = ProductSizeId
            };
            await _context.CartItems.AddAsync(newItem);
        }

        return true;
    }

    public async Task<bool> ClearCartAsync(string userId)
    {
        var cartId = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

        if (cartId == 0) return false;

        await _context.CartItems
            .Where(item => item.CartId == cartId)
            .ExecuteDeleteAsync(); // delete table by cart id

        return true;
    }

    public async Task<Cart?> GetCartByUserIdAsync(string userId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(i => i.ProductSizes)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsBySizeIdsAsync(List<int> sizeIds)
    {
        return await _context.CartItems
        .Where(ci => sizeIds.Contains(ci.ProductSizeId))
        .ToListAsync();
    }

    public async Task<int> GetItemQuantityInCartAsync(string userId, int productId, string? sizeName = null)
    {
        var cartId = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

        if (cartId == 0) return 0;

        var query = _context.CartItems.Where(i => i.CartId == cartId && i.ProductId == productId);
        if (!string.IsNullOrEmpty(sizeName))
        {
            query = query.Where(i => i.ProductSize != null && i.ProductSize.SizeName == sizeName);
        }

        return await query.Select(i => i.Quantity).FirstOrDefaultAsync(); 
    }

    public void RemoveCartItem(CartItem item)
    {
         _context.CartItems.Remove(item);
    }

    public async Task<bool> RemoveItemFromCartAsync(string userId, int productId, string? sizeName = null)
    {
        var cartId = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

        if (cartId == 0) return false;

        var query = _context.CartItems.Where(item => item.ProductId == productId && item.CartId == cartId);
        if (!string.IsNullOrEmpty(sizeName))
        {
            query = query.Where(item => item.ProductSize != null && item.ProductSize.SizeName == sizeName);
        }

        var rowAffected = await query.ExecuteDeleteAsync();
        return rowAffected > 0;
    }

    public async Task<bool> UpdateItemQuantityAsync(string userId, int productId, int quantity, string? sizeName = null)
    {
        var cartId = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

        if( cartId == 0 ) return false;
       
        var query = _context.CartItems.Where(item => item.ProductId == productId && item.CartId == cartId);
        if (!string.IsNullOrEmpty(sizeName))
        {
            query = query.Where(item => item.ProductSize != null && item.ProductSize.SizeName == sizeName);
        }

        var rowsAffected = await query.ExecuteUpdateAsync(setter => setter
                            .SetProperty(i => i.Quantity, quantity));

        return rowsAffected > 0;
    }
}
