using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sport.Application.IService;
using SportZone.API.Extensions;
using SportZone.Application.Dtos.Vnpay;
using SportZone.Application.Interfaces.IService;
using SportZone.Application.Services;
using SportZone.Domain.Enums;

namespace SportZone.API.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionController(IPromotionService promotionService) : ControllerBase
    {
        [HttpGet("active-promotions")]
        public async Task<IActionResult> GetActivePromotions()
        {
            var promotions = await promotionService.GetActivePromotionsAsync();
            return Ok(promotions);
        }
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetPromotionByCode(string code)
        {
            var promotion = await promotionService.GetByCodeAsync(code);
            if (promotion == null)
            {
                return NotFound($"Promotion with code '{code}' not found.");
            }
            return Ok(promotion);
        }
        
        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidatePromotionCode(string code, decimal orderValue) 
        {
            var discountAmount = await promotionService.IsPromotionValid(code, orderValue);
          
            return Ok(discountAmount);
        }
    }

}