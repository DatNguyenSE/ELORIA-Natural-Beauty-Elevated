using System;
using SportZone.Domain.Entities;

namespace SportZone.Application.Interfaces.IRepositories;

public interface IFeatureRepository : IGenericRepository<Feature>
{
    Task<IEnumerable<Feature>> GetBannerFeaturesAsync();
    Task<IEnumerable<Feature>> GetAllWithProduct();
    Task<Feature?> GetByIdWithProductsAsync(int id);
}