using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;

using AutoMapper;
using SportZone.Domain.Entities;

namespace SportZone.Application.Services;

public class FeatureService(IUnitOfWork uow, IMapper mapper) : IFeatureService
{
    public async Task<IEnumerable<FeatureDto>> GetAllAsync()
    {
        var features = await uow.FeatureRepository.GetAllAsync();
        return mapper.Map<IEnumerable<FeatureDto>>(features);
    }


    public async Task<IEnumerable<FeatureDto>> GetAllWithProductAsync()
    {
        var features = await uow.FeatureRepository.GetAllWithProduct();
        return mapper.Map<IEnumerable<FeatureDto>>(features);
    }

    public async Task AddAsync(CreateFeatureDto featureDto)
    {
        var entity = mapper.Map<Feature>(featureDto);
        await uow.FeatureRepository.AddAsync(entity);
        await uow.Complete();
    }

    public async Task<IEnumerable<ProductDto>> GetListByListFeatureAsync(List<int> featureIds)
    {
        var products = await uow.ProductRepository.GetListByListFeatureIdsAsync(featureIds);
        if (products == null)
        {
            throw new BadRequestException($"No products found with the specified features.");
        }

        return mapper.Map<IEnumerable<ProductDto>>(products);
    }


    public async Task DeleteAsync(int id)
    {
        var result = await uow.FeatureRepository.GetByIdAsync(id);
        if (result == null)
        {
            throw new NotFoundException($"Feature with id {id} not found.");
        }
        uow.FeatureRepository.Delete(result);
        await uow.Complete();
    }

    public async Task<IEnumerable<FeatureDto>> GetBannerFeaturesAsync()
    {
        var features = await uow.FeatureRepository.GetBannerFeaturesAsync();
        return mapper.Map<IEnumerable<FeatureDto>>(features);
    }

    public async Task<FeatureDto?> GetFeatureByIdAsync(int id)
    {
        var feature = await uow.FeatureRepository.GetByIdAsync(id);
        return mapper.Map<FeatureDto?>(feature);
    }


    public async Task UpdateAsync(int id, UpdateFeatureDto featureDto)
    {
        if (featureDto.Id != 0 && id != featureDto.Id)
        {
            throw new BadRequestException("ID trên URL và ID trong dữ liệu không khớp.");
        }
        
        // 1. Load Feature kèm Products hiện tại (Bắt buộc phải có Include)
        var entity = await uow.FeatureRepository.GetByIdWithProductsAsync(id);
        if (entity == null) throw new NotFoundException($"Feature with id {id} not found.");


        mapper.Map(featureDto, entity);

        // 3. Logic tối ưu hóa cập nhật quan hệ N-N
        var selectedIds = featureDto.ProductIds?.ToList() ?? new List<int>(); // ds tất cả id tich chọn
        var currentIds = entity.Products.Select(p => p.Id).ToList(); // ds tất cả productid đang gán feature hiện tại trong DB

        // A. Những ID có trong list mới nhưng chưa có trong DB -> Cần THÊM
        var idsToAdd = selectedIds.Except(currentIds).ToList();
        if (idsToAdd.Any())
        {
            var productsToAdd = await uow.ProductRepository.GetProductsByIdsAsync(idsToAdd);
            if (productsToAdd != null)
            {
                foreach (var p in productsToAdd)
                {
                    entity.Products.Add(p);
                }
            }
            else
            {
                throw new NotFoundException($"Some products with specified IDs not found.");
            }
        }

        // B. Những ID đang có trong DB nhưng không có trong list mới -> Cần XÓA
        var idsToRemove = currentIds.Except(selectedIds).ToList();
        if (idsToRemove.Any())
        {
            var productsToRemove = entity.Products.Where(p => idsToRemove.Contains(p.Id)).ToList();
            foreach (var p in productsToRemove)
            {
                entity.Products.Remove(p);
            }
        }

        await uow.Complete();
    }
}

//cách không tối ưu selectedIds -> search Product -> set lại entity.Products = products với selectedIds -> EF tự xóa và insert lại 