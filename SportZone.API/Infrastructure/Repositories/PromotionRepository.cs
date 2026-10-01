using SportZone.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Repositories
{
    public class PromotionRepository(AppDbContext _context) : GenericRepository<Promotion>(_context), IPromotionRepository
    {
        public Task<List<Promotion>> GetActivePromotionsAsync()
        {
            return _context.Promotions
                .Where(p => p.IsActive && (p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow))
                .ToListAsync();
        }

        public async Task<Promotion?> GetByCodeAsync(string code)
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
        }
    }
}