using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;
using AutoMapper;

namespace SportZone.Application.Services;

public class CartService(IUnitOfWork uow, IMapper mapper) : ICartService
{
    public async Task AddItemToCartAsync(string userId, int productId, int quantity, string? sizeName = null)
    {
        if (quantity <= 0) throw new BadRequestException("Số lượng phải lớn hơn 0.");

        var product = await uow.ProductRepository.GetProductByIdAsync(productId);
        if (product == null)
        {
            throw new BadRequestException("Sản phẩm không tồn tại.");
        }

        Domain.Entities.ProductSize? productSize = null;
        if (!string.IsNullOrWhiteSpace(sizeName))
        {
            productSize = await uow.ProductSizeRepository.GetProductSizeIdAsync(productId, sizeName);
        }

        if (productSize == null)
        {
            // Lấy size đầu tiên của product nếu có
            productSize = product.ProductSizes.FirstOrDefault();
            if (productSize == null)
            {
                // Tự động tạo 1 ProductSize mặc định theo dung tích và tồn kho
                productSize = new Domain.Entities.ProductSize
                {
                    ProductId = productId,
                    SizeName = product.Volume ?? "Tiêu chuẩn",
                    Quantity = product.Stock > 0 ? product.Stock : 100,
                    IsActive = true
                };
                await uow.ProductSizeRepository.AddAsync(productSize);
                await uow.Complete();
            }
        }

        // Check stock
        var hasStock = productSize.Quantity > 0 ? productSize.Quantity : (product.Stock > 0 ? product.Stock : 100);
        if (quantity > hasStock) throw new BadRequestException("Số lượng yêu cầu vượt quá tồn kho.");

        var stockInCart = await uow.CartRepository.GetItemQuantityInCartAsync(userId, productId, productSize.SizeName);
        if (quantity + stockInCart > hasStock) throw new BadRequestException("Số lượng trong giỏ hàng vượt quá tồn kho, vui lòng kiểm tra lại.");

        await uow.CartRepository.AddItemToCartAsync(userId, productId, quantity, productSize.Id);
        await uow.Complete();
    }

    public async Task ClearCartAsync(string userId)
    {
        await uow.CartRepository.ClearCartAsync(userId);
    }

    public async Task<CartDto?> GetCartByUserIdAsync(string userId)
    {
        var entity = await uow.CartRepository.GetCartByUserIdAsync(userId);
        return mapper.Map<CartDto?>(entity);
    }

    public Task RemoveItemFromCartAsync(string userId, int productId, string? sizeName = null)
    {
        return uow.CartRepository.RemoveItemFromCartAsync(userId, productId, sizeName);
    }

    public async Task UpdateItemQuantityAsync(string userId, int productId, int quantity, string? sizeName = null)
    {
        var productQuantity = await uow.CartRepository.GetItemQuantityInCartAsync(userId, productId, sizeName);
        if (productQuantity == 0) throw new BadRequestException("Sản phẩm không có trong giỏ hàng.");

        if (quantity < 0)  
        {
           throw new BadRequestException("Số lượng phải lớn hơn hoặc bằng 0.");
        }

        if (quantity == 0)
        {
            await uow.CartRepository.RemoveItemFromCartAsync(userId, productId, sizeName);
            return;
        }

        int hasStock = 100;
        if (!string.IsNullOrEmpty(sizeName))
        {
            hasStock = await uow.ProductSizeRepository.GetQuantityBySizeNameAsync(productId, sizeName);
        }
        else
        {
            var prod = await uow.ProductRepository.GetProductByIdAsync(productId);
            if (prod != null) hasStock = prod.Stock > 0 ? prod.Stock : 100;
        }
        
        if (quantity > hasStock) throw new BadRequestException("Số lượng vượt quá tồn kho hiện có.");
        
        var updated = await uow.CartRepository.UpdateItemQuantityAsync(userId, productId, quantity, sizeName);
        if (!updated)
        {
            throw new NotFoundException("Không tìm thấy giỏ hàng.");
        }
    }
}

