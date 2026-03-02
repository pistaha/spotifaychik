using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicService.API.Models;
using MusicService.API.Authorization;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Users.Commands;
using MusicService.Application.Users.Dtos;
using MusicService.Application.Users.Queries;
using MusicService.Application.Tracks.Dtos;
using MusicService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User,Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISecurityAuditService _auditService;
        private readonly IMusicServiceDbContext _dbContext;

        public UsersController(
            IMediator mediator,
            IAuthorizationService authorizationService,
            ISecurityAuditService auditService,
            IMusicServiceDbContext dbContext)
        {
            _mediator = mediator;
            _authorizationService = authorizationService;
            _auditService = auditService;
            _dbContext = dbContext;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await EnqueueAccessDeniedAsync(id, cancellationToken);
                return Forbid();
            }

            var query = new GetUserByIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
                
            return Ok(ApiResponse<UserDto>.SuccessResult(result, "User retrieved successfully"));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), 400)]
        public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> CreateUser(
            [FromBody] CreateUserCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            var response = new RegisterUserResponse
            {
                User = result,
                Token = new AuthTokenResponse()
            };

            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.UserCreated,
                result.Id,
                result.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                null,
                DateTime.UtcNow), cancellationToken);

            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, 
                ApiResponse<RegisterUserResponse>.SuccessResult(response, "User created successfully"));
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<UserDto>>>> BulkCreateUsers(
            [FromBody] List<CreateUserCommand> commands,
            CancellationToken cancellationToken = default)
        {
            var query = new BulkCreateUsersCommand { Commands = commands };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<BulkOperationResult<UserDto>>.SuccessResult(result, "Users bulk created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 404)]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
            Guid id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            if (request.FirstName != null)
            {
                user.FirstName = request.FirstName;
            }
            if (request.LastName != null)
            {
                user.LastName = request.LastName;
            }
            if (request.PhoneNumber != null)
            {
                user.PhoneNumber = request.PhoneNumber;
            }
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }
            if (request.IsEmailConfirmed.HasValue)
            {
                user.IsEmailConfirmed = request.IsEmailConfirmed.Value;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.UserUpdated,
                user.Id,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                System.Text.Json.JsonSerializer.Serialize(request),
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<UserDto>.SuccessResult(new UserDto
            {
                Id = user.Id,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                DisplayName = user.DisplayName,
                ProfileImage = user.ProfileImage,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                FavoriteGenres = user.FavoriteGenres,
                ListenTimeMinutes = user.ListenTimeMinutes,
                LastLoginAt = user.LastLoginAt,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsActive = user.IsActive
            }, "User updated"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanDeleteUser")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("User not found"));
            }

            user.IsDeleted = true;
            user.IsActive = false;
            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == id && !s.IsRevoked)
                .ToListAsync(cancellationToken);
            foreach (var session in sessions)
            {
                session.IsRevoked = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.UserDeleted,
                user.Id,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                null,
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "User deleted"));
        }

        [HttpPost("{id:guid}/block")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<ActionResult<ApiResponse<bool>>> BlockUser(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("User not found"));
            }

            user.IsActive = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.UserBlocked,
                user.Id,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                null,
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "User blocked"));
        }

        [HttpPost("{id:guid}/unblock")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<ActionResult<ApiResponse<bool>>> UnblockUser(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("User not found"));
            }

            user.IsActive = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.UserUnblocked,
                user.Id,
                user.Email,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                null,
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "User unblocked"));
        }

        [HttpPost("{id:guid}/roles")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRole(
            Guid id,
            [FromBody] RoleChangeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Role name is required"));
            }

            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);
            if (role == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Role not found"));
            }

            var exists = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == id && ur.RoleId == role.Id, cancellationToken);
            if (!exists)
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    UserId = id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = GetUserId()
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.RoleAssigned,
                id,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                System.Text.Json.JsonSerializer.Serialize(new { Role = role.Name }),
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "Role assigned"));
        }

        [HttpDelete("{id:guid}/roles/{roleName}")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeRole(
            Guid id,
            string roleName,
            CancellationToken cancellationToken = default)
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
            if (role == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Role not found"));
            }

            var userRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == role.Id, cancellationToken);
            if (userRole == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Role assignment not found"));
            }

            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.RoleRevoked,
                id,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                System.Text.Json.JsonSerializer.Serialize(new { Role = role.Name }),
                DateTime.UtcNow), cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "Role revoked"));
        }

        [HttpGet("{id:guid}/playlists")]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetUserPlaylists(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await EnqueueAccessDeniedAsync(id, cancellationToken);
                return Forbid();
            }

            var query = new GetUserPlaylistsByUserIdQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "User playlists retrieved successfully"));
        }

        [HttpPost("{id:guid}/friends/{friendId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 409)]
        public async Task<ActionResult<ApiResponse<bool>>> AddFriend(
            Guid id, 
            Guid friendId, 
            CancellationToken cancellationToken = default)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, id, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await EnqueueAccessDeniedAsync(id, cancellationToken);
                return Forbid();
            }

            var command = new AddFriendCommand 
            { 
                UserId = id, 
                FriendId = friendId 
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            if (result.Success)
            {
                return Ok(ApiResponse<bool>.SuccessResult(true, "Friend added successfully"));
            }

            return result.Status switch
            {
                AddFriendStatus.UserNotFound => NotFound(ApiResponse<bool>.ErrorResult("User not found")),
                AddFriendStatus.FriendNotFound => NotFound(ApiResponse<bool>.ErrorResult("Friend not found")),
                AddFriendStatus.AlreadyFriends => Conflict(ApiResponse<bool>.ErrorResult("Users are already friends")),
                _ => BadRequest(ApiResponse<bool>.ErrorResult("Unable to add friend"))
            };
        }

        [HttpGet("{id:guid}/statistics")]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanViewReports")]
        [ProducesResponseType(typeof(ApiResponse<UserStatisticsDto>), 200)]
        public async Task<ActionResult<ApiResponse<UserStatisticsDto>>> GetUserStatistics(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            // Admin-only per lab requirements.
            var query = new GetUserStatisticsQuery { UserId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<UserStatisticsDto>.SuccessResult(result, "User statistics retrieved successfully"));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? country = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUsersQuery 
            { 
                Page = page, 
                PageSize = pageSize,
                Search = search,
                Country = country
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResult(result, "Users retrieved successfully"));
        }

        private Task EnqueueAccessDeniedAsync(Guid targetUserId, CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var parsedUserId = Guid.TryParse(currentUserId, out var userId) ? userId : (Guid?)null;
            var details = new { TargetUserId = targetUserId };
            return _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.ResourceAccessDenied,
                parsedUserId,
                User.FindFirstValue(ClaimTypes.Email),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                false,
                System.Text.Json.JsonSerializer.Serialize(details),
                DateTime.UtcNow), cancellationToken);
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }

}
