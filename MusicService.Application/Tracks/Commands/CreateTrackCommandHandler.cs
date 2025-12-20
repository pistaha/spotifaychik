using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Tracks.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Commands
{
    public class CreateTrackCommandHandler : IRequestHandler<CreateTrackCommand, TrackDto>
    {
        private readonly ITrackRepository _trackRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTrackCommandHandler> _logger;

        public CreateTrackCommandHandler(
            ITrackRepository trackRepository,
            IAlbumRepository albumRepository,
            IArtistRepository artistRepository,
            IMapper mapper,
            ILogger<CreateTrackCommandHandler> logger)
        {
            _trackRepository = trackRepository;
            _albumRepository = albumRepository;
            _artistRepository = artistRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TrackDto> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating track: {Title}", request.Title);

            var album = await _albumRepository.GetByIdAsync(request.AlbumId, cancellationToken);
            if (album == null)
                throw new ArgumentException($"Album with ID {request.AlbumId} not found");

            var artist = await _artistRepository.GetByIdAsync(request.ArtistId, cancellationToken);
            if (artist == null)
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

            var createdTrack = await _trackRepository.CreateAsync(track, cancellationToken);
            
            _logger.LogInformation("Track {TrackId} created successfully", createdTrack.Id);
            
            return _mapper.Map<TrackDto>(createdTrack);
        }
    }
}