using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SportZone.API.Controllers
{
    [Route("api/features")]
    [ApiController]
    public class FeatureController(IFeatureService featureService) : ControllerBase
    {
        [HttpPut("{featureId:int}/update")]
        public async Task<ActionResult> UpdateFeature(int featureId, UpdateFeatureDto featureDto)
        {
            await featureService.UpdateAsync(featureId, featureDto);
            return Ok();
        }

        [HttpGet("feature-items")]
        public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetListByFeature([FromQuery] List<int> featureIds)
        {
            if (featureIds == null || !featureIds.Any())
            {
                return Ok(new List<ProductDto>());
            }
            var products = await featureService.GetListByListFeatureAsync(featureIds);
            return Ok(products);
        }

        

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FeatureDto>>> GetAllFeatures()
        {
            var features = await featureService.GetAllAsync();
            return Ok(features);
        }

        [HttpGet("with-products")]
        public async Task<ActionResult<IEnumerable<FeatureDto>>> GetAllFeaturesWithProducts()
        {
            var features = await featureService.GetAllWithProductAsync();
            return Ok(features);
        }

        [HttpPost("add")]
        public async Task<ActionResult> AddFeature(CreateFeatureDto featureDto)
        {
            await featureService.AddAsync(featureDto);
            return Ok();
        }

        [HttpDelete("{featureId:int}/delete")]
        public async Task<ActionResult> DeleteFeature(int featureId)
        {
            await featureService.DeleteAsync(featureId);
            return NoContent();
        }

        [HttpGet("feature-banners")]
        public async Task<ActionResult<IEnumerable<FeatureDto>>> GetBannerFeatures()
        {
            var features = await featureService.GetBannerFeaturesAsync();
            return Ok(features);
        }

        [HttpGet("{featureId:int}")]
        public async Task<ActionResult<FeatureDto>> GetFeatureById(int featureId)
        {
            var feature = await featureService.GetFeatureByIdAsync(featureId);
            if (feature == null)
            {
                return NotFound();
            }
            return Ok(feature);
        }
    }
}
