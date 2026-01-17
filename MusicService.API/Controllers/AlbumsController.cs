using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using System.Security.Claims;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlbumsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AlbumsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 404)]
        public async Task<ActionResult<ApiResponse<AlbumDto>>> GetAlbum(
            Guid id, 
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<AlbumDto>.ErrorResult("Invalid user"));
            }

            var query = new GetAlbumByIdQuery { AlbumId = id, UserId = IsPrivileged() ? null : userId };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(ApiResponse<AlbumDto>.ErrorResult("Album not found"));
                
            return Ok(ApiResponse<AlbumDto>.SuccessResult(result, "Album retrieved successfully"));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<AlbumDto>), 400)]
        public async Task<ActionResult<ApiResponse<AlbumDto>>> CreateAlbum(
            [FromBody] CreateAlbumCommand command, 
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var createdBy))
            {
                return Unauthorized(ApiResponse<AlbumDto>.ErrorResult("Invalid user"));
            }

            command = command with { CreatedById = createdBy };
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAlbum), new { id = result.Id }, 
                ApiResponse<AlbumDto>.SuccessResult(result, "Album created successfully"));
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<BulkOperationResult<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<BulkOperationResult<AlbumDto>>>> BulkCreateAlbums(
            [FromBody] List<CreateAlbumCommand> commands,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var createdBy))
            {
                return Unauthorized(ApiResponse<BulkOperationResult<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var payload = commands
                .Select(cmd => cmd with { CreatedById = createdBy })
                .ToList();
            var query = new BulkCreateAlbumsCommand { Commands = payload };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<BulkOperationResult<AlbumDto>>.SuccessResult(result, "Albums bulk created successfully"));
        }

        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin,Moderator")]
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
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetAlbumsByArtist(
            Guid artistId, 
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetAlbumsByArtistQuery
            {
                ArtistId = artistId,
                UserId = IsPrivileged() ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<AlbumDto>>.SuccessResult(result, "Artist albums retrieved successfully"));
        }

        [HttpGet("recent")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumDto>>>> GetRecentAlbums(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetRecentAlbumsQuery
            {
                Days = days,
                UserId = IsPrivileged() ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<List<AlbumDto>>.SuccessResult(result, "Recent albums retrieved successfully"));
        }

        [HttpGet]
        [Authorize]
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
            var userId = GetUserId();
            if (!IsPrivileged() && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<PagedResult<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetAlbumsQuery 
            { 
                Page = page, 
                PageSize = pageSize,
                Search = search,
                Genre = genre,
                SortBy = sortBy,
                SortOrder = sortOrder,
                UserId = IsPrivileged() ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<AlbumDto>>.SuccessResult(result, "Albums retrieved successfully"));
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
