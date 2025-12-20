using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Commands
{
    public class CreatePlaylistCommandHandler : IRequestHandler<CreatePlaylistCommand, PlaylistDto>
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreatePlaylistCommandHandler> _logger;

        public CreatePlaylistCommandHandler(
            IPlaylistRepository playlistRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<CreatePlaylistCommandHandler> logger)
        {
            _playlistRepository = playlistRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PlaylistDto> Handle(CreatePlaylistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating playlist: {Title}", request.Title);

            var user = await _userRepository.GetByIdAsync(request.CreatedBy, cancellationToken);
            if (user == null)
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

            var createdPlaylist = await _playlistRepository.CreateAsync(playlist, cancellationToken);
            
            _logger.LogInformation("Playlist {PlaylistId} created successfully", createdPlaylist.Id);
            
            return _mapper.Map<PlaylistDto>(createdPlaylist);
        }
    }
}