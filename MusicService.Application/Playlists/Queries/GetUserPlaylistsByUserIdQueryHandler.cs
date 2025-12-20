using AutoMapper;
using MediatR;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Queries
{
    public class GetUserPlaylistsByUserIdQueryHandler : IRequestHandler<GetUserPlaylistsByUserIdQuery, List<PlaylistDto>>
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IMapper _mapper;

        public GetUserPlaylistsByUserIdQueryHandler(
            IPlaylistRepository playlistRepository,
            IMapper mapper)
        {
            _playlistRepository = playlistRepository;
            _mapper = mapper;
        }

        public async Task<List<PlaylistDto>> Handle(GetUserPlaylistsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var playlists = await _playlistRepository.GetUserPlaylistsAsync(request.UserId, cancellationToken);
            return _mapper.Map<List<PlaylistDto>>(playlists);
        }
    }
}