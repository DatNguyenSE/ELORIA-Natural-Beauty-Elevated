using System;
using SportZone.Domain.Enums;
using SportZone.Domain.Entities;

namespace SportZone.Application.Interfaces.IRepositories;

public interface IMembersRepository : IGenericRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetMemberWithOrdersAsync(string id);
    Task<IEnumerable<AppUser>> GetAllMembersAsync();
    Task AddPointsAsync(string id, int points);
    Task<bool> SubtractPointsAsync(string id, int pointsToSubtract);

}
