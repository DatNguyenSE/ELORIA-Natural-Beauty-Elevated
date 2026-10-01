using System;
using SportZone.Domain.Entities;

namespace SportZone.Application.Interfaces.IRepositories;

public interface IProductSizeRepository : IGenericRepository<ProductSize>
{
    Task<int> GetQuantityBySizeNameAsync(int productId , string SizeName);
    Task<ProductSize?> GetProductSizeIdAsync(int productId , string SizeName);
    Task<IEnumerable<ProductSize>> GetListByProductIdsAsync(IEnumerable<int> productIDs, IEnumerable<int> productSizeIds);
    Task<ProductSize?> GetProductSizeByProductIdAndSizeNameAsync(int productId, string sizeName);
}
