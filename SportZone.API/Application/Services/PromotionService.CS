using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos;
using System.Threading.Tasks;

namespace SportZone.Application.Services
{
    public class PromotionService(IUnitOfWork uow, IMapper mapper) : IPromotionService
    {
        public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync()
        {
            var promotions = await uow.PromotionRepository.GetActivePromotionsAsync();
            return mapper.Map<IEnumerable<PromotionDto>>(promotions);
        }

        public async Task<PromotionDto?> GetByCodeAsync(string code)
        {
            var promotion = await uow.PromotionRepository.GetByCodeAsync(code);
            return mapper.Map<PromotionDto>(promotion);
        }

        public async Task<decimal?> IsPromotionValid(string code, decimal orderValue)
        {
            var activePromotion = await uow.PromotionRepository.GetByCodeAsync(code);
             
            if (activePromotion == null)
            {
                return null;
            }

            var currentDate = DateTime.UtcNow;
            return activePromotion.IsActive &&
                   activePromotion.StartDate <= currentDate &&
                   activePromotion.EndDate >= currentDate &&
                   activePromotion.MinOrderValue <= orderValue  ? activePromotion.MaxDiscountAmount : null;      
        }
    }

}