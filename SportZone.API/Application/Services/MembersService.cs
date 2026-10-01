using System;
using SportZone.Application.Dtos;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Exceptions;

using AutoMapper;
using SportZone.Domain.Entities;
using SportZone.Application.Dtos.SportZone.Application.Dtos;

namespace SportZone.Application.Services;

public class MembersService(IUnitOfWork uow, IMapper mapper) : IMembersService
{
    public Task AddAsync(UpdateProfileDto userDto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<MemberDto>> GetAllAsync()
    {
        var members = await uow.MembersRepository.GetAllMembersAsync();
        if(members == null)
        {
            throw new NotFoundException("No members found.");
        }
        var memberDtos = mapper.Map<IEnumerable<MemberDto>>(members);
        return memberDtos;
    }
    
    public async Task<MemberDto?> GetMemberDetailByIdAsync(string id)
    {
        if(string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Member ID cannot be null or empty.", nameof(id));
        }
        
        var member = await uow.MembersRepository.GetMemberWithOrdersAsync(id);
        if(member == null)
        {
            throw new NotFoundException($"Member with ID {id} not found.");
        }
        var memberDto = mapper.Map<MemberDto>(member);
        return memberDto;
    }

    public Task UpdateAsync(string id, UpdateProfileDto memberDto)
    {
        throw new NotImplementedException();
    }

    public async Task SubtractPointsAsync(string id, int pointsToSubtract)
    {
       bool result = await uow.MembersRepository.SubtractPointsAsync(id, pointsToSubtract);
      if (!result) throw new BadRequestException("Bạn không đủ điểm để thực hiện giao dịch này.");

      await uow.Complete();
    }
}