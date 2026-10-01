using System;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;
using System.Linq.Expressions;

namespace SportZone.Infrastructure.Repositories;

public class ProductSizeRepository(AppDbContext _context) : GenericRepository<ProductSize>(_context), IProductSizeRepository
{


    public async Task<IEnumerable<ProductSize>> GetListByProductIdsAsync(IEnumerable<int> productIds, IEnumerable<int> productSizeIds)
    {
        return await _context.ProductSizes
                        .Where(i => productIds.Contains(i.ProductId) && productSizeIds.Contains(i.Id))
                        .ToListAsync();
    }

    public Task<ProductSize?> GetProductSizeByProductIdAndSizeNameAsync(int productId, string sizeName)
    {
        return _context.ProductSizes
                     .FirstOrDefaultAsync(i => i.ProductId == productId && i.SizeName == sizeName);
    }

    public async Task<ProductSize?> GetProductSizeIdAsync(int productId, string sizeName)
    {
        return await _context.ProductSizes
                     .FirstOrDefaultAsync(i => i.ProductId == productId && i.SizeName == sizeName);
    }

    public async Task<int> GetQuantityBySizeNameAsync(int productId, string sizeName)
    {
        var productSize = await _context.ProductSizes
                        .Where(i => i.ProductId == productId && i.SizeName == sizeName)
                        .FirstOrDefaultAsync();

        if (productSize == null)
        {
            return -1;
        }
        return productSize.Quantity;
    }

}
