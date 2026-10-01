using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Repositories;

public class FeatureRepository(AppDbContext _context) : GenericRepository<Feature>(_context), IFeatureRepository
{
    public async Task<IEnumerable<Feature>> GetBannerFeaturesAsync()
    {
        return await _context.Features
            .AsNoTracking()
            .Where(f => f.IsBanner == true)
            .ToListAsync();
    }
    public async Task<IEnumerable<Feature>> GetAllWithProduct()
    {
        return await _context.Features
            .AsNoTracking()
            .Include(f => f.Products)
            .ToListAsync();
    }
    
    public async Task<Feature?> GetByIdWithProductsAsync(int id)
    {
        return await _context.Features
            .Include(f => f.Products)
            .FirstOrDefaultAsync(f => f.Id == id);
    }


}