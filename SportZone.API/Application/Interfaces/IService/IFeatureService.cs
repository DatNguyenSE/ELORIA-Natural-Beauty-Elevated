using System;
using SportZone.Application.Dtos;

namespace SportZone.Application.Interfaces.IService;

public interface IFeatureService
{
    Task<IEnumerable<FeatureDto>> GetAllAsync();
    Task AddAsync(CreateFeatureDto featureDto);
    Task<IEnumerable<ProductDto>> GetListByListFeatureAsync(List<int> featureIds);
    Task<IEnumerable<FeatureDto>> GetBannerFeaturesAsync();
    Task UpdateAsync(int id, UpdateFeatureDto featureDto);
    Task DeleteAsync(int id);
    Task<FeatureDto?> GetFeatureByIdAsync(int id);
    Task<IEnumerable<FeatureDto>> GetAllWithProductAsync();

}