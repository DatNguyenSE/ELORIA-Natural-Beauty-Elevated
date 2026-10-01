using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;
using SportZone.Domain.Entities;

namespace SportZone.Application.Extensions
{
    public static class AppUserExtensions
{
    public static async Task<UserDto> ToDto(this AppUser user, ITokenService tokenService)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email!,
            ImageUrl = user.ImageUrl,
            Points = user.Points,
            Token = await tokenService.CreateToken(user)
        };
    }
}
}