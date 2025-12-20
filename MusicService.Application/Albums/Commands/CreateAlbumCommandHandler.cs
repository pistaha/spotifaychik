using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Albums.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class CreateAlbumCommandHandler : IRequestHandler<CreateAlbumCommand, AlbumDto>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateAlbumCommandHandler> _logger;

        public CreateAlbumCommandHandler(
            IAlbumRepository albumRepository,
            IArtistRepository artistRepository,
            IMapper mapper,
            ILogger<CreateAlbumCommandHandler> logger)
        {
            _albumRepository = albumRepository;
            _artistRepository = artistRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AlbumDto> Handle(CreateAlbumCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating album: {Title}", request.Title);

            var artist = await _artistRepository.GetByIdAsync(request.ArtistId, cancellationToken);
            if (artist == null)
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

            var createdAlbum = await _albumRepository.CreateAsync(album, cancellationToken);
            
            _logger.LogInformation("Album {AlbumId} created successfully", createdAlbum.Id);
            
            return _mapper.Map<AlbumDto>(createdAlbum);
        }
    }
}