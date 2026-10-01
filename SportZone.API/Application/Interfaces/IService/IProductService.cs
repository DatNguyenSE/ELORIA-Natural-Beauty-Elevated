using System;
using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos;

namespace SportZone.Application.Interfaces.IService;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto> AddAsync(CreateProductDto createDto);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductDto productDto, IFormFile? file);
    Task<IEnumerable<ProductDto>> GetListByCategoryIdAsync(int categoryId);
    Task DeleteAsync(int id);
    Task<IEnumerable<ProductDto>> GetListByLabelAsync(string label);
    Task<IEnumerable<ProductDto>> GetListByFeatureIdAsync(int featureId);
}
