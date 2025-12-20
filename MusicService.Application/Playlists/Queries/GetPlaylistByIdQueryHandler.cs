using AutoMapper;
using MediatR;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Queries
{
    public class GetPlaylistByIdQueryHandler : IRequestHandler<GetPlaylistByIdQuery, PlaylistDto?>
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IMapper _mapper;

        public GetPlaylistByIdQueryHandler(
            IPlaylistRepository playlistRepository,
            IMapper mapper)
        {
            _playlistRepository = playlistRepository;
            _mapper = mapper;
        }

        public async Task<PlaylistDto?> Handle(GetPlaylistByIdQuery request, CancellationToken cancellationToken)
        {
            var playlist = await _playlistRepository.GetByIdAsync(request.PlaylistId, cancellationToken);
            
            if (playlist == null)
                return null;

            var dto = _mapper.Map<PlaylistDto>(playlist);
            
            // Проверяем, подписан ли пользователь на плейлист
            if (request.UserId.HasValue && playlist.Followers != null)
            {
                dto.IsFollowing = playlist.Followers.Any(f => f.Id == request.UserId.Value);
            }

            return dto;
        }
    }
}