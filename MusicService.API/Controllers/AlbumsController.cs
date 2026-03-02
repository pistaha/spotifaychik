using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MusicService.Application.Albums.Commands;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Albums.Queries;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MusicService.API.Models;
using MusicService.Domain.Entities;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlbumsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMusicServiceDbContext _dbContext;

        public AlbumsController(IMediator mediator, IMusicServiceDbContext dbContext)
        {
            _mediator = mediator;
            _dbContext = dbContext;
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
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<AlbumDto>.ErrorResult("Invalid user"));
            }

            var query = new GetAlbumByIdQuery
            {
                AlbumId = id,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
            };
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
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetAlbumsByArtistQuery
            {
                ArtistId = artistId,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
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
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<AlbumDto>>.ErrorResult("Invalid user"));
            }

            var query = new GetRecentAlbumsQuery
            {
                Days = days,
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
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
            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && !userId.HasValue)
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
                UserId = (User.IsInRole("Admin") || User.IsInRole("Moderator")) ? null : userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResponse<PagedResult<AlbumDto>>.SuccessResult(result, "Albums retrieved successfully"));
        }

        [HttpGet("{id:guid}/images")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumImageDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumImageDto>>>> GetAlbumImages(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var album = await _dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Images)
                    .ThenInclude(ai => ai.File)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (album == null)
            {
                return NotFound(ApiResponse<List<AlbumImageDto>>.ErrorResult("Album not found"));
            }

            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && album.CreatedById != GetUserId())
            {
                return Forbid();
            }

            var result = album.Images
                .OrderBy(ai => ai.Order)
                .Select(ai => new AlbumImageDto
                {
                    FileId = ai.FileId,
                    IsMain = ai.IsMain,
                    Order = ai.Order,
                    FileUrl = Url.Action("DownloadFile", "Files", new { id = ai.FileId }) ?? $"/api/files/{ai.FileId}",
                    ThumbnailUrl = Url.Action("Thumbnail", "Files", new { id = ai.FileId, size = "small" }),
                    FileSize = ai.File?.Size ?? 0,
                    FileType = ai.File?.ContentType ?? string.Empty
                })
                .ToList();

            return Ok(ApiResponse<List<AlbumImageDto>>.SuccessResult(result, "Album images retrieved"));
        }

        [HttpPost("{id:guid}/images")]
        [Authorize(Roles = "Admin,Moderator")]
        [ProducesResponseType(typeof(ApiResponse<List<AlbumImageDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<AlbumImageDto>>>> AddAlbumImages(
            Guid id,
            [FromBody] List<AlbumImageRequest> images,
            CancellationToken cancellationToken = default)
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(ApiResponse<List<AlbumImageDto>>.ErrorResult("Images are required"));
            }

            var album = await _dbContext.Albums.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (album == null)
            {
                return NotFound(ApiResponse<List<AlbumImageDto>>.ErrorResult("Album not found"));
            }

            var fileIds = images.Select(i => i.FileId).ToList();
            var files = await _dbContext.FileMetadatas
                .Where(f => fileIds.Contains(f.Id))
                .ToListAsync(cancellationToken);

            if (files.Count != fileIds.Count)
            {
                return BadRequest(ApiResponse<List<AlbumImageDto>>.ErrorResult("Some files were not found"));
            }

            var currentUserId = GetUserId();
            foreach (var file in files)
            {
                if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(ApiResponse<List<AlbumImageDto>>.ErrorResult("Only images are allowed"));
                }

                if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")) && file.UploadedBy != currentUserId)
                {
                    return Forbid();
                }
            }

            foreach (var request in images)
            {
                var exists = await _dbContext.AlbumImages.AnyAsync(ai => ai.AlbumId == id && ai.FileId == request.FileId, cancellationToken);
                if (exists)
                {
                    continue;
                }

                _dbContext.AlbumImages.Add(new AlbumImage
                {
                    AlbumId = id,
                    FileId = request.FileId,
                    IsMain = request.IsMain,
                    Order = request.Order,
                    AttachedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var albumImages = await _dbContext.AlbumImages
                .AsNoTracking()
                .Include(ai => ai.File)
                .Where(ai => ai.AlbumId == id)
                .OrderBy(ai => ai.Order)
                .ToListAsync(cancellationToken);

            var result = albumImages
                .Select(ai => new AlbumImageDto
                {
                    FileId = ai.FileId,
                    IsMain = ai.IsMain,
                    Order = ai.Order,
                    FileUrl = Url.Action("DownloadFile", "Files", new { id = ai.FileId }) ?? $"/api/files/{ai.FileId}",
                    ThumbnailUrl = Url.Action("Thumbnail", "Files", new { id = ai.FileId, size = "small" }),
                    FileSize = ai.File?.Size ?? 0,
                    FileType = ai.File?.ContentType ?? string.Empty
                })
                .ToList();

            return Ok(ApiResponse<List<AlbumImageDto>>.SuccessResult(result, "Album images updated"));
        }

        [HttpDelete("{id:guid}/images/{fileId:guid}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAlbumImage(
            Guid id,
            Guid fileId,
            [FromQuery] bool deleteFile = false,
            CancellationToken cancellationToken = default)
        {
            var image = await _dbContext.AlbumImages.FirstOrDefaultAsync(ai => ai.AlbumId == id && ai.FileId == fileId, cancellationToken);
            if (image == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Image not found"));
            }

            _dbContext.AlbumImages.Remove(image);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (deleteFile)
            {
                var file = await _dbContext.FileMetadatas.FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);
                if (file != null)
                {
                    _dbContext.FileMetadatas.Remove(file);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return Ok(ApiResponse<bool>.SuccessResult(true, "Image removed"));
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }

    }
}
