using System;
using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos;

namespace SportZone.Application.Interfaces.IService;

public interface IPromotionService
{
    Task<PromotionDto?> GetByCodeAsync(string code);
    Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync();
    Task<decimal?> IsPromotionValid(string code, decimal orderValue);
}