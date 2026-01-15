using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common;
using MusicService.Application.Tracks.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Commands
{
    public class CreateTrackCommandHandler : IRequestHandler<CreateTrackCommand, TrackDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTrackCommandHandler> _logger;

        public CreateTrackCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreateTrackCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TrackDto> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating track: {Title}", request.Title);

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

                    var albumExists = await _dbContext.Albums
                        .AsNoTracking()
                        .AnyAsync(a => a.Id == request.AlbumId, cancellationToken);
                    if (!albumExists)
                        throw new ArgumentException($"Album with ID {request.AlbumId} not found");

                    var artistExists = await _dbContext.Artists
                        .AsNoTracking()
                        .AnyAsync(a => a.Id == request.ArtistId, cancellationToken);
                    if (!artistExists)
                        throw new ArgumentException($"Artist with ID {request.ArtistId} not found");

                    var track = new Track
                    {
                        Title = request.Title,
                        DurationSeconds = request.DurationSeconds,
                        Lyrics = request.Lyrics,
                        AudioFileUrl = request.AudioFileUrl,
                        TrackNumber = request.TrackNumber,
                        IsExplicit = request.IsExplicit,
                        AlbumId = request.AlbumId,
                        ArtistId = request.ArtistId,
                        PlayCount = 0,
                        LikeCount = 0
                    };

                    _dbContext.Tracks.Add(track);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    _logger.LogInformation("Track {TrackId} created successfully", track.Id);
                    return _mapper.Map<TrackDto>(track);
                }
                catch (DbUpdateException ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsUniqueViolation(ex))
                    {
                        throw new ArgumentException("Track with the same number already exists for this album");
                    }

                    if (DatabaseErrorDetector.IsForeignKeyViolation(ex))
                    {
                        throw new ArgumentException("Album or artist not found");
                    }

                    if (DatabaseErrorDetector.IsTransient(ex) && attempt < maxAttempts)
                    {
                        await DelayAsync(attempt, cancellationToken);
                        continue;
                    }

                    throw;
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

                    throw;
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }

            throw new InvalidOperationException("Failed to create track after multiple attempts.");
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }
    }
}
