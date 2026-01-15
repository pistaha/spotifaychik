using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class BulkCreateAlbumsCommandHandler : IRequestHandler<BulkCreateAlbumsCommand, BulkOperationResult<AlbumDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateAlbumsCommandHandler> _logger;

        public BulkCreateAlbumsCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<BulkCreateAlbumsCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<AlbumDto>> Handle(BulkCreateAlbumsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bulk creating {Count} albums", request.Commands.Count);

            var result = new BulkOperationResult<AlbumDto>();
            var albumsToCreate = new List<Album>();

            foreach (var command in request.Commands)
            {
                try
                {
                    var artistExists = await _dbContext.Artists
                        .AsNoTracking()
                        .AnyAsync(a => a.Id == command.ArtistId, cancellationToken);
                    if (!artistExists)
                    {
                        result.Items.Add(new BulkOperationItem<AlbumDto>
                        {
                            Success = false,
                            Message = $"Artist with ID {command.ArtistId} not found",
                            Error = "Artist not found"
                        });
                        continue;
                    }

                    var album = new Album
                    {
                        Title = command.Title,
                        Description = command.Description,
                        CoverImage = command.CoverImage,
                        ReleaseDate = command.ReleaseDate,
                        Type = Enum.Parse<AlbumType>(command.Type),
                        Genres = command.Genres,
                        ArtistId = command.ArtistId
                    };

                    albumsToCreate.Add(album);
                    result.Items.Add(new BulkOperationItem<AlbumDto>
                    {
                        Success = true,
                        Message = "Album queued for creation",
                        Data = _mapper.Map<AlbumDto>(album)
                    });
                }
                catch (Exception ex)
                {
                    result.Items.Add(new BulkOperationItem<AlbumDto>
                    {
                        Success = false,
                        Message = "Error processing album",
                        Error = ex.Message
                    });
                }
            }

            var createdAlbums = new List<AlbumDto>();
            if (albumsToCreate.Count > 0)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    _dbContext.Albums.AddRange(albumsToCreate);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    createdAlbums.AddRange(albumsToCreate.Select(a => _mapper.Map<AlbumDto>(a)));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error creating albums in bulk operation");
                }
            }

            result.TotalCount = request.Commands.Count;
            result.SuccessfulCount = createdAlbums.Count;
            result.FailedCount = result.TotalCount - result.SuccessfulCount;

            _logger.LogInformation("Bulk album creation completed: {SuccessfulCount} successful, {FailedCount} failed", 
                result.SuccessfulCount, result.FailedCount);

            return result;
        }
    }
}
