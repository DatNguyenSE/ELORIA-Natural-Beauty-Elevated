using SportZone.API.Extensions;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SportZone.API.Controllers
{
    [Route("api/cart")]
    [ApiController]
    [Authorize]
    public class CartController(ICartService cartService) : ControllerBase
    {
        [HttpPost("add")]
        public async Task<IActionResult> AddItemToCart(AddCartItemDto dto)
        {
            var userId =User.GetUserId(); 
            await cartService.AddItemToCartAsync(userId, dto.ProductId, dto.Quantity, dto.SizeName);
            return Ok();
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.GetUserId();
            await cartService.ClearCartAsync(userId);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.GetUserId();
            var cart =  await cartService.GetCartByUserIdAsync(userId);
            return Ok(cart);
        }

        [HttpDelete("remove/{productId:int}")]
        public async Task<IActionResult> RemoveItemFromCart(int productId, [FromQuery] string? sizeName = null)
        {
            var userId = User.GetUserId();
            await cartService.RemoveItemFromCartAsync(userId, productId, sizeName);
            return NoContent();
        }
        
        [HttpPost("update-item")]
        public async Task<IActionResult> UpdateItemQuantity(UpdateCartItemDto dto)
        {
            var userId = User.GetUserId();
            await cartService.UpdateItemQuantityAsync(userId, dto.ProductId, dto.Quantity, dto.SizeName);
            return Ok();
        }
    }
}
