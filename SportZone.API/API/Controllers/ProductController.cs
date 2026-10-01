using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController(IProductService productService, IPhotoService photoService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts()
        {
            var products = await productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{productId:int}")]
        public async Task<IActionResult> GetProductById(int productId)
        {
            var product = await productService.GetByIdAsync(productId);
            return Ok(product);
        }

        // [HttpGet("CATCH/{productId:int}")]
        // public async Task<IActionResult> GetProductById(int productId)
        // {
        //     var product = await productService.GetByIdAsync(productId);
        //     return Ok(product);
        // }

        [HttpGet("category/{categoryId:int}")]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetListByCategoryId(int categoryId)
        {
            var products = await productService.GetListByCategoryIdAsync(categoryId);
            return Ok(products);
        }

        [HttpPost("add")]
        public async Task<ActionResult<ProductDto>> AddProduct(CreateProductDto productDto)
        {

            var productAdded = await productService.AddAsync(productDto);

            return CreatedAtAction(nameof(GetProductById), new { productId = productAdded.Id }, productAdded);

        }

        [HttpPost("add_image")]
        public async Task<ActionResult<ProductDto>> AddProductWithImage( [FromForm] CreateProductDto productDto, IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please, choose pic for product!.");
            }
            var result = await photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error);

            productDto.ImageUrl = result.Url;
            var productAdded = await productService.AddAsync(productDto);

            return CreatedAtAction(nameof(GetProductById), new { productId = productAdded.Id }, productAdded);

        }

        [HttpPut("update/{productId:int}")]
        public async Task<ActionResult> UpdateProduct(int productId,[FromForm] UpdateProductDto productDto, IFormFile? file)
        {
            var product = await productService.UpdateAsync(productId, productDto, file);
            return Ok(product);

        }

        [HttpDelete("delete/{productId:int}")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            await productService.DeleteAsync(productId);
            return NoContent();
        }

        [HttpGet("label-items/{label}")]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetListByLabel(string label)
        {
            var products = await productService.GetListByLabelAsync(label);
            return Ok(products);
        }
        [HttpGet("feature-items/{featureId:int}")]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProductsByFeature(int featureId)
        {
            var products = await productService.GetListByFeatureIdAsync(featureId);
            return Ok(products);
        }

        
    }
}

