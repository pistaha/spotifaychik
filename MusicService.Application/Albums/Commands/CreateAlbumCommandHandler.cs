using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Albums.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
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

            var artistExists = await _dbContext.Artists
                .AsNoTracking()
                .AnyAsync(a => a.Id == request.ArtistId, cancellationToken);
            if (!artistExists)
                throw new ArgumentException($"Artist with ID {request.ArtistId} not found");

            var album = new Album
            {
                Title = request.Title,
                Description = request.Description,
                CoverImage = request.CoverImage,
                ReleaseDate = request.ReleaseDate,
                Type = Enum.Parse<AlbumType>(request.Type),
                Genres = request.Genres,
                ArtistId = request.ArtistId
            };

            _dbContext.Albums.Add(album);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Album {AlbumId} created successfully", album.Id);
            
            return _mapper.Map<AlbumDto>(album);
        }
    }
}
