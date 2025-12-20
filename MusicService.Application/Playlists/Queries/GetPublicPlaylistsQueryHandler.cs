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
    public class GetPublicPlaylistsQueryHandler : IRequestHandler<GetPublicPlaylistsQuery, List<PlaylistDto>>
    {
        private readonly IPlaylistRepository _playlistRepository;
        private readonly IMapper _mapper;

        public GetPublicPlaylistsQueryHandler(
            IPlaylistRepository playlistRepository,
            IMapper mapper)
        {
            _playlistRepository = playlistRepository;
            _mapper = mapper;
        }

        public async Task<List<PlaylistDto>> Handle(GetPublicPlaylistsQuery request, CancellationToken cancellationToken)
        {
            var playlists = await _playlistRepository.GetPublicPlaylistsAsync(cancellationToken);
            
            // Сортировка
            playlists = request.SortBy?.ToLower() switch
            {
                "title" => request.SortOrder == "asc" ? 
                    playlists.OrderBy(p => p.Title).ToList() : 
                    playlists.OrderByDescending(p => p.Title).ToList(),
                "createdat" => request.SortOrder == "asc" ?
                    playlists.OrderBy(p => p.CreatedAt).ToList() :
                    playlists.OrderByDescending(p => p.CreatedAt).ToList(),
                "followerscount" => request.SortOrder == "asc" ?
                    playlists.OrderBy(p => p.FollowersCount).ToList() :
                    playlists.OrderByDescending(p => p.FollowersCount).ToList(),
                _ => request.SortOrder == "asc" ?
                    playlists.OrderBy(p => p.Title).ToList() :
                    playlists.OrderByDescending(p => p.Title).ToList()
            };

            // Лимит
            if (request.Limit.HasValue && request.Limit.Value > 0)
            {
                playlists = playlists.Take(request.Limit.Value).ToList();
            }

            return _mapper.Map<List<PlaylistDto>>(playlists);
        }
    }
}