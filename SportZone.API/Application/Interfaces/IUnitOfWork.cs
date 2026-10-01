using System;
using SportZone.Application.Interfaces.IRepositories;

namespace SportZone.Application.Interfaces;

public interface IUnitOfWork
{
    IProductRepository ProductRepository { get; }
    IProductSizeRepository ProductSizeRepository { get; }
    ICategoryRepository CategoryRepository { get; }
    ICartRepository CartRepository { get; }
    IOrderRepository OrderRepository { get; }
    IPromotionRepository PromotionRepository { get; }
    IFeatureRepository FeatureRepository { get; }
    IMembersRepository MembersRepository { get; }
    Task<bool> Complete();
    bool HasChange();
}
