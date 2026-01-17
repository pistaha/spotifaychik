using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Common;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Commands;
using MusicService.Application.Tracks.Queries;
using System.Security.Claims;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TracksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TracksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 404)]
        public async Task<ActionResult<ApiResponse<TrackDto>>> GetTrack(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<TrackDto>.ErrorResult("Invalid user"));
            }

            var query = new GetTrackByIdQuery { TrackId = id, UserId = IsPrivileged() ? null : userId };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<TrackDto>.ErrorResult("Track not found"));
                
            return Ok(ApiResponse<TrackDto>.SuccessResult(result, "Track retrieved successfully"));
        }

        [HttpGet("album/{albumId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<TrackDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<TrackDto>>>> GetTracksByAlbum(
            Guid albumId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<TrackDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetTracksByAlbumQuery
            {
                AlbumId = albumId,
                UserId = IsPrivileged() ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<TrackDto>>.SuccessResult(result, "Album tracks retrieved successfully"));
        }

        [HttpGet("top")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<TrackDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<TrackDto>>>> GetTopTracks(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<TrackDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetTopTracksQuery
            {
                Count = count,
                UserId = IsPrivileged() ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<TrackDto>>.SuccessResult(result, "Top tracks retrieved successfully"));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 400)]
        public async Task<ActionResult<ApiResponse<TrackDto>>> CreateTrack(
            [FromBody] CreateTrackCommand command,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var createdBy))
            {
                return Unauthorized(ApiResponse<TrackDto>.ErrorResult("Invalid user"));
            }

            command = command with { CreatedById = createdBy };
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetTrack), new { id = result.Id },
                ApiResponse<TrackDto>.SuccessResult(result, "Track created successfully"));
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        private bool IsPrivileged()
        {
            return User.IsInRole("Admin") || User.IsInRole("Moderator");
        }
    }
}
