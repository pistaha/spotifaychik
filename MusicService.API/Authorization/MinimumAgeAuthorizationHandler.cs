using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MusicService.API.Authorization
{
    public class MinimumAgeAuthorizationHandler : AuthorizationHandler<MinimumAgeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
        {
            var dobClaim = context.User.FindFirst("DateOfBirth")?.Value;
            if (string.IsNullOrWhiteSpace(dobClaim))
            {
                return Task.CompletedTask;
            }

            if (!DateTime.TryParse(dobClaim, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dob))
            {
                return Task.CompletedTask;
            }

            var age = DateTime.UtcNow.Year - dob.Year;
            if (DateTime.UtcNow.Date < dob.Date.AddYears(age))
            {
                age--;
            }

            if (age >= requirement.Age)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
