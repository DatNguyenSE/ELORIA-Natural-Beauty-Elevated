using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;

namespace SportZone.API.Controllers
{
    [Route("api/Categories")]
    [ApiController]
    public class CategoryController(ICategoryService categoryService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories()
        {
            var categories = await categoryService.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{categoryId:int}")]
        public async Task<ActionResult<CategoryDto>> GetById(int categoryId)
        {
            var categories = await categoryService.GetByIdAsync(categoryId);
            return Ok(categories);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            if (string.IsNullOrWhiteSpace(categoryDto.CategoryName))
                return BadRequest("Tên thể loại không được để trống.");

            var created = await categoryService.CreateAsync(categoryDto);
            return CreatedAtAction(nameof(GetById), new { categoryId = created.Id }, created);
        }

        [HttpDelete("{categoryId:int}")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var success = await categoryService.DeleteAsync(categoryId);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
