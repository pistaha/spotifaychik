using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MusicService.API.Authorization
{
    public sealed class ResourceOwnerAuthorizationHandler
        : AuthorizationHandler<ResourceOwnerRequirement, Guid>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement,
            Guid resource)
        {
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Moderator"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userId, out var currentUserId) && currentUserId == resource)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
