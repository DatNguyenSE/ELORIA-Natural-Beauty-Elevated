using System;
using SportZone.Application.Dtos;


namespace SportZone.Application.Interfaces.IService;

public interface IProductSizeService
{
    Task UpdateProductSizeAsync(int productSizeId, int quantity);
    Task<ProductSizeDto?> GetProductSizeAsync(int productSizeId);
    Task<int> GetQuantityBySizeNameAsync(int productId, string sizeName);
    
}
