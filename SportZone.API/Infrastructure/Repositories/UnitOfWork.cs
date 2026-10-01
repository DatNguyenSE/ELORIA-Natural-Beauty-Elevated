using System;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;

namespace SportZone.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext _context): IUnitOfWork
{
    private IProductRepository? _productRepository;
    private ProductSizeRepository? _productSizeRepository;
    private ICartRepository? _cartRepository;
    private IOrderRepository? _orderRepository;
    private ICategoryRepository? _categoryRepository;
    private IPromotionRepository? _promotionRepository;
    private IFeatureRepository? _featureRepository;
    private IMembersRepository? _membersRepository;
    
//when other function call (uow.ProductRepository) -> check and avoid create multiple instance
    public IProductRepository ProductRepository => _productRepository 
        ??= new ProductRepository(_context);

    public IProductSizeRepository ProductSizeRepository => _productSizeRepository 
        ??= new ProductSizeRepository(_context);
    public ICartRepository CartRepository => _cartRepository 
        ??= new CartRepository(_context);

    public IOrderRepository OrderRepository => _orderRepository
        ??= new OrderRepository(_context);
    public ICategoryRepository CategoryRepository => _categoryRepository
        ??= new CategoryRepository(_context);

    public IPromotionRepository PromotionRepository => _promotionRepository 
        ??= new PromotionRepository(_context);

    public IFeatureRepository FeatureRepository => _featureRepository
        ??= new FeatureRepository(_context);

    public IMembersRepository MembersRepository => _membersRepository
        ??= new MembersRepository(_context);    
    public async Task<bool> Complete()
    {
        try
        {
            return await _context.SaveChangesAsync() > 0; //Sửa dữ liệu trên RAM -> Chờ lệnh Save -> EF sinh SQL -> Gửi DB
        }
        catch(DbUpdateException ex)
        {
            throw new Exception("An error occured while saving changes", ex);
        }
    }

    public bool HasChange()
    {
        return _context.ChangeTracker.HasChanges();
    }
}
