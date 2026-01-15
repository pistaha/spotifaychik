using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Artists.Commands;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Artists.Queries;
using MusicService.Application.Common;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArtistsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ArtistsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 404)]
        public async Task<ActionResult<ApiResponse<ArtistDto>>> GetArtist(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetArtistByIdQuery { ArtistId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<ArtistDto>.ErrorResult("Artist not found"));
                
            return Ok(ApiResponse<ArtistDto>.SuccessResult(result, "Artist retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ArtistDto>), 400)]
        public async Task<ActionResult<ApiResponse<ArtistDto>>> CreateArtist(
            [FromBody] CreateArtistCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetArtist), new { id = result.Id }, 
                ApiResponse<ArtistDto>.SuccessResult(result, "Artist created successfully"));
        }

        [HttpGet("top")]
        [ProducesResponseType(typeof(ApiResponse<List<ArtistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ArtistDto>>>> GetTopArtists(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTopArtistsQuery { Count = count };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ArtistDto>>.SuccessResult(result, "Top artists retrieved successfully"));
        }

        [HttpGet("genre/{genre}")]
        [ProducesResponseType(typeof(ApiResponse<List<ArtistDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ArtistDto>>>> GetArtistsByGenre(
            string genre,
            CancellationToken cancellationToken = default)
        {
            var query = new GetArtistsByGenreQuery { Genre = genre };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<ArtistDto>>.SuccessResult(result, $"Artists in genre '{genre}' retrieved successfully"));
        }
    }
}
