using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SportZone.API.Controllers
{
    [Route("api/ProductSizes")]
    [ApiController]
    public class ProductSizeController(IProductSizeService productSizeService) : ControllerBase
    {
        [HttpPut("{productSizeId:int}/update")]
        public async Task<ActionResult> UpdateInventory(int productSizeId, int quantity)
        {
            await productSizeService.UpdateProductSizeAsync(productSizeId, quantity);
            return Ok();
        }
        

        [HttpGet("{productId:int}/quantity")]
        public async Task<ActionResult<int>> GetQuantity(int productId, string sizeName)
        {
            var quantity = await productSizeService.GetQuantityBySizeNameAsync(productId, sizeName);
            return Ok(quantity);
        }
    }
}
