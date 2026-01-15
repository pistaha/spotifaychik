using System.Security.Claims;

namespace MusicService.API.Authentication
{
    public interface IJwtTokenService
    {
        string CreateToken(IEnumerable<Claim> claims);
    }
}
