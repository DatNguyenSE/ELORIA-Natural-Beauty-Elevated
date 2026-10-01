using System;
using Microsoft.AspNetCore.Http;
using SportZone.Application.Dtos;
using SportZone.Application.Dtos.SportZone.Application.Dtos;

namespace SportZone.Application.Interfaces.IService;

public interface IMembersService
{
    Task<IEnumerable<MemberDto>> GetAllAsync();
    Task<MemberDto?> GetMemberDetailByIdAsync(string id);
    Task AddAsync(UpdateProfileDto userDto);
    Task UpdateAsync(string id, UpdateProfileDto memberDto);
    Task DeleteAsync(string id);
    Task SubtractPointsAsync(string id, int pointsToSubtract);
}