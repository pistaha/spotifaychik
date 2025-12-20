using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class BulkDeleteAlbumsCommandHandler : IRequestHandler<BulkDeleteAlbumsCommand, BulkDeleteResult>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly ILogger<BulkDeleteAlbumsCommandHandler> _logger;

        public BulkDeleteAlbumsCommandHandler(
            IAlbumRepository albumRepository,
            ILogger<BulkDeleteAlbumsCommandHandler> logger)
        {
            _albumRepository = albumRepository;
            _logger = logger;
        }

        public async Task<BulkDeleteResult> Handle(BulkDeleteAlbumsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bulk deleting {Count} albums", request.AlbumIds.Count);

            var result = new BulkDeleteResult
            {
                TotalCount = request.AlbumIds.Count
            };

            foreach (var albumId in request.AlbumIds)
            {
                try
                {
                    var album = await _albumRepository.GetByIdAsync(albumId, cancellationToken);
                    if (album == null)
                    {
                        result.Items.Add(new BulkDeleteItem
                        {
                            Id = albumId,
                            Success = false,
                            Message = "Album not found",
                            Error = "Album not found"
                        });
                        result.FailedCount++;
                        continue;
                    }

                    var deleted = await _albumRepository.DeleteAsync(albumId, cancellationToken);
                    if (deleted)
                    {
                        result.Items.Add(new BulkDeleteItem
                        {
                            Id = albumId,
                            Success = true,
                            Message = "Album deleted successfully"
                        });
                        result.SuccessfulCount++;
                    }
                    else
                    {
                        result.Items.Add(new BulkDeleteItem
                        {
                            Id = albumId,
                            Success = false,
                            Message = "Failed to delete album",
                            Error = "Delete operation failed"
                        });
                        result.FailedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting album {AlbumId} in bulk operation", albumId);
                    result.Items.Add(new BulkDeleteItem
                    {
                        Id = albumId,
                        Success = false,
                        Message = "Error deleting album",
                        Error = ex.Message
                    });
                    result.FailedCount++;
                }
            }

            _logger.LogInformation("Bulk album deletion completed: {SuccessfulCount} successful, {FailedCount} failed", 
                result.SuccessfulCount, result.FailedCount);

            return result;
        }
    }
}