using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class CreateAlbumCommandHandler : IRequestHandler<CreateAlbumCommand, AlbumDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateAlbumCommandHandler> _logger;

        public CreateAlbumCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreateAlbumCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AlbumDto> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating album: {Title}", request.Title);

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

                    var artistExists = await _dbContext.Artists
                        .AsNoTracking()
                        .AnyAsync(a => a.Id == request.ArtistId, cancellationToken);
                    if (!artistExists)
                        throw new ArgumentException($"Artist with ID {request.ArtistId} not found");

                    var creatorExists = await _dbContext.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == request.CreatedById, cancellationToken);
                    if (!creatorExists)
                        throw new ArgumentException($"User with ID {request.CreatedById} not found");

                    var album = new Album
                    {
                        Title = request.Title,
                        Description = request.Description,
                        CoverImage = request.CoverImage,
                        ReleaseDate = request.ReleaseDate,
                        Type = Enum.Parse<AlbumType>(request.Type),
                        Genres = request.Genres,
                        ArtistId = request.ArtistId,
                        CreatedById = request.CreatedById
                    };

                    _dbContext.Albums.Add(album);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    if (transaction != null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                    }

                    _logger.LogInformation("Album {AlbumId} created successfully", album.Id);
                    return _mapper.Map<AlbumDto>(album);
                }
                catch (DbUpdateException ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    if (DatabaseErrorDetector.IsUniqueViolation(ex))
                    {
                        throw new ArgumentException("Album with the same artist and title already exists");
                    }

                    if (DatabaseErrorDetector.IsForeignKeyViolation(ex))
                    {
                        throw new ArgumentException($"Artist with ID {request.ArtistId} not found");
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

            throw new InvalidOperationException("Failed to create album after multiple attempts.");
        }

        private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
        {
            var delayMs = 50 * (int)Math.Pow(2, attempt - 1);
            return Task.Delay(delayMs, cancellationToken);
        }
    }
}
