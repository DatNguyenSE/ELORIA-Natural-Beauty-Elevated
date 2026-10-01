using SportZone.Domain.Entities;


namespace SportZone.Application.Interfaces.IService
{
    public interface ITokenService
    {
        Task<string> CreateToken(AppUser user);
        string GenerateRefreshToken();
    }
}