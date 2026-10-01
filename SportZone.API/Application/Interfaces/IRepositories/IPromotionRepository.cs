
using SportZone.Domain.Entities;

namespace SportZone.Application.Interfaces
{

    public interface IPromotionRepository : IGenericRepository<Promotion>
    {
        Task<Promotion?> GetByCodeAsync(string code);
        Task<List<Promotion>> GetActivePromotionsAsync();

    }
}