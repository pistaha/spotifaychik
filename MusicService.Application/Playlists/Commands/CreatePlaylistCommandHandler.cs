using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Commands
{
    public class CreatePlaylistCommandHandler : IRequestHandler<CreatePlaylistCommand, PlaylistDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreatePlaylistCommandHandler> _logger;

        public CreatePlaylistCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreatePlaylistCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PlaylistDto> Handle(CreatePlaylistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating playlist: {Title}", request.Title);

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

                    var userExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == request.CreatedBy, cancellationToken);
                    if (!userExists)
                        throw new ArgumentException($"User with ID {request.CreatedBy} not found");

                    var playlist = new Playlist
                    {
                        Title = request.Title,
                        Description = request.Description,
                        CoverImage = request.CoverImage,
                        IsPublic = request.IsPublic,
                        IsCollaborative = request.IsCollaborative,
                        Type = Enum.Parse<PlaylistType>(request.Type),
                        CreatedById = request.CreatedBy,
                        FollowersCount = 0,
                        TotalDurationMinutes = 0
                    };

                    _dbContext.Playlists.Add(playlist);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    _logger.LogInformation("Playlist {PlaylistId} created successfully", playlist.Id);
                    return _mapper.Map<PlaylistDto>(playlist);
                }
                catch (DbUpdateException ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsUniqueViolation(ex))
                    {
                        throw new ArgumentException("Playlist with the same title already exists for this user");
                    }

                    if (DatabaseErrorDetector.IsForeignKeyViolation(ex))
                    {
                        throw new ArgumentException($"User with ID {request.CreatedBy} not found");
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

            throw new InvalidOperationException("Failed to create playlist after multiple attempts.");
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }
    }
}
