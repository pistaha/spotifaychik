using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.API.Authorization;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Interfaces;
using MusicService.Domain.Entities;
using System.Security.Claims;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISecurityAuditService _auditService;
        private readonly IAuthorizationService _authorizationService;

        public PlaylistsController(
            IMediator mediator,
            ISecurityAuditService auditService,
            IAuthorizationService authorizationService)
        {
            _mediator = mediator;
            _auditService = auditService;
            _authorizationService = authorizationService;
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 404)]
        public async Task<ActionResult<ApiResponse<PlaylistDto>>> GetPlaylist(
            Guid id,
            [FromQuery] Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Moderator");
            if (!isPrivileged)
            {
                var currentUserId = GetUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized(ApiResponse<PlaylistDto>.ErrorResult("Invalid user"));
                }

                userId = currentUserId;
            }

            var query = new GetPlaylistByIdQuery 
            { 
                PlaylistId = id,
                UserId = userId,
                IncludePrivate = true,
                AllowPrivateAccess = isPrivileged
            };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<PlaylistDto>.ErrorResult("Playlist not found"));
                
            return Ok(ApiResponse<PlaylistDto>.SuccessResult(result, "Playlist retrieved successfully"));
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetPlaylists(
            [FromQuery] Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Moderator");
            if (!isPrivileged)
            {
                var currentUserId = GetUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized(ApiResponse<List<PlaylistDto>>.ErrorResult("Invalid user"));
                }

                userId = currentUserId;
            }

            var query = new GetPlaylistsQuery
            {
                UserId = userId,
                IncludePrivate = true
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "Playlists retrieved successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        [Authorize(Policy = "CanEditPost")]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 404)]
        public async Task<ActionResult<ApiResponse<PlaylistDto>>> UpdatePlaylist(
            Guid id,
            [FromBody] UpdatePlaylistCommand command,
            CancellationToken cancellationToken = default)
        {
            var existing = await _mediator.Send(new GetPlaylistByIdQuery
            {
                PlaylistId = id,
                IncludePrivate = true,
                AllowPrivateAccess = true
            }, cancellationToken);
            if (existing == null)
            {
                return NotFound(ApiResponse<PlaylistDto>.ErrorResult("Playlist not found"));
            }

            var authResult = await _authorizationService.AuthorizeAsync(User, existing.CreatedById, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await _auditService.EnqueueAsync(new SecurityAuditEntry(
                    SecurityEventType.ResourceAccessDenied,
                    GetUserId(),
                    User.FindFirstValue(ClaimTypes.Email),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    false,
                    System.Text.Json.JsonSerializer.Serialize(new { TargetPlaylistId = id }),
                    DateTime.UtcNow), cancellationToken);
                return Forbid();
            }

            command = command with { PlaylistId = id };
            var result = await _mediator.Send(command, cancellationToken);
            if (result == null)
            {
                return NotFound(ApiResponse<PlaylistDto>.ErrorResult("Playlist not found"));
            }

            return Ok(ApiResponse<PlaylistDto>.SuccessResult(result, "Playlist updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> DeletePlaylist(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var existing = await _mediator.Send(new GetPlaylistByIdQuery
            {
                PlaylistId = id,
                IncludePrivate = true,
                AllowPrivateAccess = true
            }, cancellationToken);
            if (existing == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Playlist not found"));
            }

            var authResult = await _authorizationService.AuthorizeAsync(User, existing.CreatedById, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await _auditService.EnqueueAsync(new SecurityAuditEntry(
                    SecurityEventType.ResourceAccessDenied,
                    GetUserId(),
                    User.FindFirstValue(ClaimTypes.Email),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    false,
                    System.Text.Json.JsonSerializer.Serialize(new { TargetPlaylistId = id }),
                    DateTime.UtcNow), cancellationToken);
                return Forbid();
            }

            var deleted = await _mediator.Send(new DeletePlaylistCommand { PlaylistId = id }, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResult(deleted, deleted ? "Playlist deleted" : "Playlist not found"));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 400)]
        public async Task<ActionResult<ApiResponse<PlaylistDto>>> CreatePlaylist(
            [FromBody] CreatePlaylistCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var createdBy))
            {
                return Unauthorized(ApiResponse<PlaylistDto>.ErrorResult("Invalid user"));
            }

            command = command with { CreatedBy = createdBy };
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetPlaylist), new { id = result.Id },
                ApiResponse<PlaylistDto>.SuccessResult(result, "Playlist created successfully"));
        }

        [HttpGet("user/{userId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetUserPlaylists(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User, userId, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                await _auditService.EnqueueAsync(new SecurityAuditEntry(
                    SecurityEventType.ResourceAccessDenied,
                    GetUserId(),
                    User.FindFirstValue(ClaimTypes.Email),
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    false,
                    System.Text.Json.JsonSerializer.Serialize(new { TargetUserId = userId }),
                    DateTime.UtcNow), cancellationToken);
                return Forbid();
            }

            var query = new GetUserPlaylistsQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "User playlists retrieved successfully"));
        }

        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetPublicPlaylists(
            CancellationToken cancellationToken = default)
        {
            var query = new GetPublicPlaylistsQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "Public playlists retrieved successfully"));
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }
    }
}
