using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlbumsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AlbumsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 404)]
        public async Task<ActionResult<ApiResponse<AlbumDto>>> GetAlbum(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetAlbumByIdQuery { AlbumId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<AlbumDto>.ErrorResult("Album not found"));
                
            return Ok(ApiResponse<AlbumDto>.SuccessResult(result, "Album retrieved successfully"));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 400)]
        public async Task<ActionResult<ApiResponse<AlbumDto>>> CreateAlbum(
            [FromBody] CreateAlbumCommand command, 
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAlbum), new { id = result.Id }, 
                ApiResponse<AlbumDto>.SuccessResult(result, "Album created successfully"));
        }

        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<AlbumDto>>>> BulkCreateAlbums(
            [FromBody] List<CreateAlbumCommand> commands,
            CancellationToken cancellationToken = default)
        {
            var query = new BulkCreateAlbumsCommand { Commands = commands };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<BulkOperationResult<AlbumDto>>.SuccessResult(result, "Albums bulk created successfully"));
        }

        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkDeleteResult>), 200)]
        public async Task<ActionResult<ApiResponse<BulkDeleteResult>>> BulkDeleteAlbums(
            [FromBody] List<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var query = new BulkDeleteAlbumsCommand { AlbumIds = ids };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<BulkDeleteResult>.SuccessResult(result, "Albums bulk deleted successfully"));
        }

        [HttpGet("artist/{artistId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetAlbumsByArtist(
            Guid artistId, 
            CancellationToken cancellationToken = default)
        {
            var query = new GetAlbumsByArtistQuery { ArtistId = artistId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<AlbumDto>>.SuccessResult(result, "Artist albums retrieved successfully"));
        }

        [HttpGet("recent")]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetRecentAlbums(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            var query = new GetRecentAlbumsQuery { Days = days };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<AlbumDto>>.SuccessResult(result, "Recent albums retrieved successfully"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<AlbumDto>>>> GetAlbums(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortOrder = "desc",
            CancellationToken cancellationToken = default)
        {
            var query = new GetAlbumsQuery 
            { 
                Page = page, 
                PageSize = pageSize,
                Search = search,
                Genre = genre,
                SortBy = sortBy,
                SortOrder = sortOrder
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<AlbumDto>>.SuccessResult(result, "Albums retrieved successfully"));
        }
    }
}
