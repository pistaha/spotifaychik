using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
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

            var result = new BulkOperationResult<AlbumDto>
            {
                TotalCount = request.Commands.Count
            };

            var commandsToProcess = request.Commands.ToList();
            var maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                IDbContextTransaction? transaction = null;
                var attemptItems = new List<BulkOperationItem<AlbumDto>>();
                var successfulCount = 0;
                var processedCommands = 0;

                try
                {
                    var isInMemory = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
                    var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
                    if (!isInMemory)
                    {
                        transaction = await _dbContext.Database.BeginTransactionAsync(
                            System.Data.IsolationLevel.Serializable, cancellationToken);
                    }

                    var supportsSavepoints = transaction?.SupportsSavepoints == true;
                    var artistIds = commandsToProcess.Select(c => c.ArtistId).Distinct().ToList();
                    var existingArtistIds = await _dbContext.Artists
                        .AsNoTracking()
                        .Where(a => artistIds.Contains(a.Id))
                        .Select(a => a.Id)
                        .ToListAsync(cancellationToken);
                    var existingSet = new HashSet<Guid>(existingArtistIds);

                    var itemIndex = 0;
                    foreach (var command in commandsToProcess)
                    {
                        processedCommands++;
                        var savepointName = $"album_{itemIndex++}";
                        if (supportsSavepoints && transaction != null)
                        {
                            await transaction.CreateSavepointAsync(savepointName, cancellationToken);
                        }

                        if (!existingSet.Contains(command.ArtistId))
                        {
                            if (supportsSavepoints && transaction != null)
                            {
                                await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                            }
                            attemptItems.Add(new BulkOperationItem<AlbumDto>
                            {
                                Success = false,
                                Message = $"Artist with ID {command.ArtistId} not found",
                                Error = "Artist not found"
                            });
                            continue;
                        }

                        Album? album = null;
                        try
                        {
                            if (!Enum.TryParse<AlbumType>(command.Type, true, out var albumType))
                            {
                                _logger.LogWarning("Invalid album type {Type} for Title={Title} ArtistId={ArtistId}",
                                    command.Type, command.Title, command.ArtistId);
                                if (supportsSavepoints && transaction != null)
                                {
                                    await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                                }
                                attemptItems.Add(new BulkOperationItem<AlbumDto>
                                {
                                    Success = false,
                                    Message = $"Invalid album type {command.Type}",
                                    Error = "Invalid album type"
                                });
                                continue;
                            }

                            var now = DateTime.UtcNow;
                            var genres = command.Genres ?? new List<string>();
                            album = new Album
                            {
                                Id = Guid.NewGuid(),
                                Title = command.Title,
                                Description = command.Description,
                                CoverImage = command.CoverImage,
                                ReleaseDate = command.ReleaseDate,
                                Type = albumType,
                                Genres = genres,
                                ArtistId = command.ArtistId,
                                TotalDurationMinutes = 0,
                                CreatedAt = now,
                                UpdatedAt = now
                            };

                            if (isPostgres)
                            {
                                var rows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO albums (""Id"", ""Title"", ""Description"", ""CoverImage"", ""ReleaseDate"", ""Type"", ""Genres"", ""TotalDurationMinutes"", ""ArtistId"", ""CreatedAt"", ""UpdatedAt"")
VALUES ({album.Id}, {album.Title}, {album.Description}, {album.CoverImage}, {album.ReleaseDate}, {(int)album.Type}, {genres.ToArray()}, {album.TotalDurationMinutes}, {album.ArtistId}, {album.CreatedAt}, {album.UpdatedAt})
ON CONFLICT DO NOTHING;");
                                if (rows == 0)
                                {
                                    if (supportsSavepoints && transaction != null)
                                    {
                                        await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                                    }
                                    attemptItems.Add(new BulkOperationItem<AlbumDto>
                                    {
                                        Success = false,
                                        Message = $"Album {command.Title} already exists",
                                        Error = "Album already exists"
                                    });
                                    continue;
                                }
                            }
                            else
                            {
                                _dbContext.Albums.Add(album);
                                await _dbContext.SaveChangesAsync(cancellationToken);
                            }

                            if (supportsSavepoints && transaction != null)
                            {
                                await transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                            }

                            attemptItems.Add(new BulkOperationItem<AlbumDto>
                            {
                                Success = true,
                                Message = "Album created successfully",
                                Data = _mapper.Map<AlbumDto>(album)
                            });
                            successfulCount++;
                        }
                        catch (Exception ex)
                        {
                            if (supportsSavepoints && transaction != null)
                            {
                                await transaction.RollbackToSavepointAsync(savepointName, cancellationToken);
                            }

                            if (DatabaseErrorDetector.IsTransient(ex))
                            {
                                throw;
                            }

                            if (!isPostgres && album != null)
                            {
                                if (_dbContext is DbContext context)
                                {
                                    context.Entry(album).State = EntityState.Detached;
                                }
                            }

                            _logger.LogWarning(ex, "Error processing album payload Title={Title} ArtistId={ArtistId}",
                                command.Title, command.ArtistId);
                            attemptItems.Add(new BulkOperationItem<AlbumDto>
                            {
                                Success = false,
                                Message = $"Error processing album {command.Title}",
                                Error = DatabaseErrorDetector.IsUniqueViolation(ex)
                                    ? "Album already exists"
                                    : DatabaseErrorDetector.IsForeignKeyViolation(ex)
                                        ? "Artist not found"
                                        : ex.Message
                            });
                        }
                    }

                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    result.Items = attemptItems;
                    result.SuccessfulCount = successfulCount;
                    result.FailedCount = result.TotalCount - result.SuccessfulCount;

                    _logger.LogInformation("Bulk album creation completed: {SuccessfulCount} successful, {FailedCount} failed", 
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

                    _logger.LogError(ex, "Error creating albums in bulk operation");
                    for (var index = processedCommands; index < commandsToProcess.Count; index++)
                    {
                        var command = commandsToProcess[index];
                        attemptItems.Add(new BulkOperationItem<AlbumDto>
                        {
                            Success = false,
                            Message = $"Error creating album {command.Title}",
                            Error = ex.Message
                        });
                    }

                    result.Items = attemptItems;
                    result.SuccessfulCount = successfulCount;
                    result.FailedCount = result.TotalCount - result.SuccessfulCount;
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
