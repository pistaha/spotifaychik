using System.Security.Claims;

namespace MusicService.API.Authentication
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(IEnumerable<Claim> claims, DateTime? expiresAt = null);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
