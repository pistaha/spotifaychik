using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

            var albums = await _dbContext.Albums
                .Where(a => request.AlbumIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            var foundIds = new HashSet<Guid>(albums.Select(a => a.Id));
            foreach (var albumId in request.AlbumIds)
            {
                if (foundIds.Contains(albumId))
                {
                    continue;
                }

                result.Items.Add(new BulkDeleteItem
                {
                    Id = albumId,
                    Success = false,
                    Message = "Album not found",
                    Error = "Album not found"
                });
                result.FailedCount++;
            }

            if (result.FailedCount > 0)
            {
                _logger.LogInformation("Bulk album deletion completed: {SuccessfulCount} successful, {FailedCount} failed",
                    result.SuccessfulCount, result.FailedCount);
                return result;
            }

            IDbContextTransaction? transaction = null;
            try
            {
                var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                if (!isInMemory)
                {
                    transaction = await _dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable, cancellationToken);
                }

                _dbContext.Albums.RemoveRange(albums);
                await _dbContext.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                foreach (var albumId in request.AlbumIds)
                {
                    result.Items.Add(new BulkDeleteItem
                    {
                        Id = albumId,
                        Success = true,
                        Message = "Album deleted successfully"
                    });
                    result.SuccessfulCount++;
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _logger.LogError(ex, "Error deleting albums in bulk operation");
                foreach (var albumId in request.AlbumIds)
                {
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
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }

            _logger.LogInformation("Bulk album deletion completed: {SuccessfulCount} successful, {FailedCount} failed",
                result.SuccessfulCount, result.FailedCount);

            return result;
        }
    }
}
