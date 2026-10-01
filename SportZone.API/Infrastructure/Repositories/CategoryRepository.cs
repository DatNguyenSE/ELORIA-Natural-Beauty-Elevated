using SportZone.Application.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;
using SportZone.Infrastructure.Data;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext _context) : GenericRepository<Category>(_context), ICategoryRepository
{
    
}