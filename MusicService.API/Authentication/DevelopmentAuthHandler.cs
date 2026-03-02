using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MusicService.API.Authentication
{
    public class DevelopmentAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
#pragma warning disable CS0618
        public DevelopmentAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
#pragma warning restore CS0618
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var devUserId = "00000000-0000-0000-0000-000000000001";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, devUserId),
                new Claim(ClaimTypes.Name, "Development User"),
                new Claim(ClaimTypes.Email, "dev@local"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("EmailConfirmed", "true")
            }, Scheme.Name);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
