using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class BulkDeleteAlbumsCommandHandler : IRequestHandler<BulkDeleteAlbumsCommand, BulkDeleteResult>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<BulkDeleteAlbumsCommandHandler> _logger;

        public BulkDeleteAlbumsCommandHandler(
            IMusicServiceDbContext dbContext,
            ILogger<BulkDeleteAlbumsCommandHandler> logger)
        {
            _dbContext = dbContext;
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
                    var album = await _dbContext.Albums
                        .FirstOrDefaultAsync(a => a.Id == albumId, cancellationToken);
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

                    _dbContext.Albums.Remove(album);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    result.Items.Add(new BulkDeleteItem
                    {
                        Id = albumId,
                        Success = true,
                        Message = "Album deleted successfully"
                    });
                    result.SuccessfulCount++;
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
