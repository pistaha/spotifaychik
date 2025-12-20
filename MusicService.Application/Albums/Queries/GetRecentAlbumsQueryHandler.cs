using AutoMapper;
using MediatR;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetRecentAlbumsQueryHandler : IRequestHandler<GetRecentAlbumsQuery, List<AlbumDto>>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IMapper _mapper;

        public GetRecentAlbumsQueryHandler(
            IAlbumRepository albumRepository,
            IMapper mapper)
        {
            _albumRepository = albumRepository;
            _mapper = mapper;
        }

        public async Task<List<AlbumDto>> Handle(GetRecentAlbumsQuery request, CancellationToken cancellationToken)
        {
            var albums = await _albumRepository.GetRecentReleasesAsync(request.Days, cancellationToken);
            return _mapper.Map<List<AlbumDto>>(albums);
        }
    }
}