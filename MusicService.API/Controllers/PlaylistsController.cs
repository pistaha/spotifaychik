using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Playlists.Commands;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Playlists.Queries;
using MusicService.Application.Common;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlaylistsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlaylistsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 404)]
        public async Task<ActionResult<ApiResponse<PlaylistDto>>> GetPlaylist(
            Guid id,
            [FromQuery] Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPlaylistByIdQuery 
            { 
                PlaylistId = id,
                UserId = userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<PlaylistDto>.ErrorResult("Playlist not found"));
                
            return Ok(ApiResponse<PlaylistDto>.SuccessResult(result, "Playlist retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<PlaylistDto>), 400)]
        public async Task<ActionResult<ApiResponse<PlaylistDto>>> CreatePlaylist(
            [FromBody] CreatePlaylistCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetPlaylist), new { id = result.Id },
                ApiResponse<PlaylistDto>.SuccessResult(result, "Playlist created successfully"));
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetUserPlaylists(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserPlaylistsQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "User playlists retrieved successfully"));
        }

        [HttpGet("public")]
        [ProducesResponseType(typeof(ApiResponse<List<PlaylistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<PlaylistDto>>>> GetPublicPlaylists(
            CancellationToken cancellationToken = default)
        {
            var query = new GetPublicPlaylistsQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<PlaylistDto>>.SuccessResult(result, "Public playlists retrieved successfully"));
        }
    }
}
