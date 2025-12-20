using AutoMapper;
using MediatR;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Queries
{
    public class GetUserPlaylistsQueryHandler : IRequestHandler<GetUserPlaylistsQuery, List<PlaylistDto>>
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IMapper _mapper;

        public GetUserPlaylistsQueryHandler(
            IPlaylistRepository playlistRepository,
            IMapper mapper)
        {
            _playlistRepository = playlistRepository;
            _mapper = mapper;
        }

        public async Task<List<PlaylistDto>> Handle(GetUserPlaylistsQuery request, CancellationToken cancellationToken)
        {
            var playlists = await _playlistRepository.GetUserPlaylistsAsync(request.UserId, cancellationToken);
            
            // Фильтруем приватные плейлисты, если не включен флаг
            if (!request.IncludePrivate)
            {
                playlists = playlists.Where(p => p.IsPublic).ToList();
            }

            return _mapper.Map<List<PlaylistDto>>(playlists);
        }
    }
}