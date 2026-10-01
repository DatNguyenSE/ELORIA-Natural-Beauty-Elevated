using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;

using AutoMapper;

namespace SportZone.Application.Services;

public class CategoryService(IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await uow.CategoryRepository.GetAllAsync();
        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> GetByIdAsync(int categoryId)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(categoryId);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CategoryDto categoryDto)
    {
        var category = mapper.Map<SportZone.Domain.Entities.Category>(categoryDto);
        await uow.CategoryRepository.AddAsync(category);
        await uow.Complete();
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<bool> DeleteAsync(int categoryId)
    {
        var category = await uow.CategoryRepository.GetByIdAsync(categoryId);
        if (category == null) return false;
        uow.CategoryRepository.Delete(category);
        return await uow.Complete();
    }
}