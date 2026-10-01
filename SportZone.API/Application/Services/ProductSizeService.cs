using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IRepositories;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;
using AutoMapper;
using SportZone.Domain.Entities;

namespace SportZone.Application.Services;

public class ProductSizeService(IUnitOfWork uow, IMapper mapper) : IProductSizeService
{
    public async Task UpdateProductSizeAsync(int productSizeId, int quantity)
    {
        if (quantity < 0)
        {
            throw new BadRequestException("Quantity cannot be negative.");
        }

        var productSize = await uow.ProductSizeRepository.GetByIdAsync(productSizeId) //ef core tracked entity
            ?? throw new NotFoundException("Product size not found."); 

        productSize.Quantity = quantity;

        await uow.Complete();
    }

    public async Task<ProductSizeDto?> GetProductSizeAsync(int productSizeId)
    {
        var productSize =  await uow.ProductSizeRepository.GetByIdAsync(productSizeId)
            ?? throw new NotFoundException("Product size not found.");
         
        return mapper.Map<ProductSizeDto>(productSize);
    }

    public async Task<int> GetQuantityBySizeNameAsync(int productId, string sizeName)
    {
        var productExists = await uow.ProductRepository.AnyAsync(p => p.Id == productId);
        if (!productExists) throw new NotFoundException("Product not found.");
        
        var quantity = await uow.ProductSizeRepository.GetQuantityBySizeNameAsync(productId, sizeName);
        if(quantity == -1)
        {
            throw new NotFoundException("Product size not found.");
        }
        return quantity;
    }
}