using SportZone.Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;
namespace SportZone.Infrastructure.Repositories
{
    public class ProductRepository(AppDbContext _context) : GenericRepository<Product>(_context), IProductRepository
    {
        public async Task<IEnumerable<Product?>> GetListByCategoryIdAsync(int id)
        {
            return await _context.Products
             .Where(p => p.CategoryId == id)
             .Include(p => p.ProductSizes)
             .ToListAsync();
        }
        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.ComboItems)
                    .ThenInclude(ci => ci.ComponentProduct)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task<IEnumerable<Product?>> GetAllProductsDetailAsync()
        {
            return await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.ComboItems)
                    .ThenInclude(ci => ci.ComponentProduct)
                .ToListAsync();
        }
        public async Task<bool> ChangeStatusProduct(int id) // soft delete
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return false;
            }

            product.IsDeleted = true;
            return true;
        }

        public async Task<IEnumerable<Product?>> GetListByLabelAsync(string label)
        {
            return await _context.Products
                .Where(p => p.Label == label)
                 .Include(p => p.ProductSizes)
                 .ToListAsync();
        }

        public async Task<IEnumerable<Product?>> GetListByListFeatureIdsAsync(List<int> featureIds)
        {
            return await _context.Products
               .AsNoTracking()
               .Include(p => p.Features.Where(f => featureIds.Contains(f.Id)))
               .Include(p => p.ProductSizes)
               .Where(p => p.Features.Any(f => featureIds.Contains(f.Id)))
               .ToListAsync();
        }
        public async Task<IEnumerable<Product?>> GetListByFeatureIdAsync(int featureId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Features.Any(f => f.Id == featureId)) // Lọc sản phẩm
                .Include(p => p.Features.Where(f => f.Id == featureId)) 
                .ToListAsync();
        }

        public async Task<IEnumerable<Product?>> GetProductsByIdsAsync(List<int> productIds)
        {
            return await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Include(p => p.ProductSizes)
                .ToListAsync();
        }
    }
}
