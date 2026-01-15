using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Common;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Tracks.Queries;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TracksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TracksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<TrackDto>), 404)]
        public async Task<ActionResult<ApiResponse<TrackDto>>> GetTrack(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetTrackByIdQuery { TrackId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<TrackDto>.ErrorResult("Track not found"));
                
            return Ok(ApiResponse<TrackDto>.SuccessResult(result, "Track retrieved successfully"));
        }

        [HttpGet("album/{albumId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<TrackDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<TrackDto>>>> GetTracksByAlbum(
            Guid albumId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTracksByAlbumQuery { AlbumId = albumId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<TrackDto>>.SuccessResult(result, "Album tracks retrieved successfully"));
        }

        [HttpGet("top")]
        [ProducesResponseType(typeof(ApiResponse<List<TrackDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<TrackDto>>>> GetTopTracks(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetTopTracksQuery { Count = count };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<TrackDto>>.SuccessResult(result, "Top tracks retrieved successfully"));
        }
    }
}
