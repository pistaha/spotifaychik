using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Common.Interfaces;
using System.Security.Claims;

namespace MusicService.API.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public PermissionAuthorizationHandler(IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return;
            }

            var hasPermission = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => _dbContext.RolePermissions.Where(rp => rp.RoleId == ur.RoleId))
                .Join(_dbContext.Permissions,
                    rp => rp.PermissionId,
                    p => p.Id,
                    (_, p) => p.Name)
                .AnyAsync(name => name == requirement.PermissionName);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
