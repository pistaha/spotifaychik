using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
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

            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                    if (!isInMemory)
                    {
                        transaction = await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable, cancellationToken);
                    }

                    var requestedIds = request.AlbumIds.ToList();
                    var distinctIds = requestedIds.Distinct().ToList();

                    var initialExistingIds = await _dbContext.Albums
                        .AsNoTracking()
                        .Where(a => distinctIds.Contains(a.Id))
                        .Select(a => a.Id)
                        .ToListAsync(cancellationToken);
                    var initialExistingSet = new HashSet<Guid>(initialExistingIds);

                    try
                    {
                        if (isInMemory)
                        {
                            var albums = await _dbContext.Albums
                                .Where(a => distinctIds.Contains(a.Id))
                                .ToListAsync(cancellationToken);
                            _dbContext.Albums.RemoveRange(albums);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                        else
                        {
                            await _dbContext.Albums
                                .Where(a => distinctIds.Contains(a.Id))
                                .ExecuteDeleteAsync(cancellationToken);
                        }
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Recalculate results based on current state if a concurrency conflict happens.
                    }

                    var remainingIds = await _dbContext.Albums
                        .AsNoTracking()
                        .Where(a => distinctIds.Contains(a.Id))
                        .Select(a => a.Id)
                        .ToListAsync(cancellationToken);
                    var remainingSet = new HashSet<Guid>(remainingIds);
                    var localItems = new List<BulkDeleteItem>();
                    var deletedCount = 0;
                    var notFoundCount = 0;
                    var notDeletedCount = 0;
                    foreach (var albumId in requestedIds)
                    {
                        if (!initialExistingSet.Contains(albumId))
                        {
                            localItems.Add(new BulkDeleteItem
                            {
                                Id = albumId,
                                Success = false,
                                Message = "Album not found",
                                Error = "Album not found"
                            });
                            notFoundCount++;
                            continue;
                        }

                        if (remainingSet.Contains(albumId))
                        {
                            localItems.Add(new BulkDeleteItem
                            {
                                Id = albumId,
                                Success = false,
                                Message = "Album could not be deleted",
                                Error = "Album still exists after delete attempt"
                            });
                            notDeletedCount++;
                            continue;
                        }

                        localItems.Add(new BulkDeleteItem
                        {
                            Id = albumId,
                            Success = true,
                            Message = "Album deleted successfully"
                        });
                        deletedCount++;
                    }

                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    result.Items = localItems;
                    result.SuccessfulCount = deletedCount;
                    result.FailedCount = notFoundCount + notDeletedCount;

                    _logger.LogInformation("Bulk album deletion completed: {SuccessfulCount} successful, {FailedCount} failed",
                        result.SuccessfulCount, result.FailedCount);

                    return result;
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    _logger.LogError(ex, "Error deleting albums in bulk operation");
                    result.Items = request.AlbumIds.Select(albumId => new BulkDeleteItem
                        {
                            Id = albumId,
                            Success = false,
                            Message = "Error deleting album",
                            Error = ex.Message
                        })
                        .ToList();
                    result.FailedCount = result.Items.Count;

                    _logger.LogInformation("Bulk album deletion completed: {SuccessfulCount} successful, {FailedCount} failed",
                        result.SuccessfulCount, result.FailedCount);
                    return result;
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }

            return result;
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }
    }
}
