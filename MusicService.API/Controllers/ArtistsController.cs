using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common;
using System.Security.Claims;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtistsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ArtistsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 404)]
        public async Task<ActionResult<ApiResponse<ArtistDto>>> GetArtist(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<ArtistDto>.ErrorResult("Invalid user"));
            }

            var query = new GetArtistByIdQuery
            {
                ArtistId = id,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<ArtistDto>.ErrorResult("Artist not found"));
                
            return Ok(ApiResponse<ArtistDto>.SuccessResult(result, "Artist retrieved successfully"));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 400)]
        public async Task<ActionResult<ApiResponse<ArtistDto>>> CreateArtist(
            [FromBody] CreateArtistCommand command, 
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var createdBy))
            {
                return Unauthorized(ApiResponse<ArtistDto>.ErrorResult("Invalid user"));
            }

            command = command with { CreatedById = createdBy };
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetArtist), new { id = result.Id }, 
                ApiResponse<ArtistDto>.SuccessResult(result, "Artist created successfully"));
        }

        [HttpGet("top")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ArtistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ArtistDto>>>> GetTopArtists(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<ArtistDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetTopArtistsQuery
            {
                Count = count,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ArtistDto>>.SuccessResult(result, "Top artists retrieved successfully"));
        }

        [HttpGet("genre/{genre}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<ArtistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ArtistDto>>>> GetArtistsByGenre(
            string genre,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<ArtistDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetArtistsByGenreQuery
            {
                Genre = genre,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ArtistDto>>.SuccessResult(result, $"Artists in genre '{genre}' retrieved successfully"));
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }

    }
}
