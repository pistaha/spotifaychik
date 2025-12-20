using AutoMapper;
using MediatR;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumsByArtistQueryHandler : IRequestHandler<GetAlbumsByArtistQuery, List<AlbumDto>>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IMapper _mapper;

        public GetAlbumsByArtistQueryHandler(
            IAlbumRepository albumRepository,
            IMapper mapper)
        {
            _albumRepository = albumRepository;
            _mapper = mapper;
        }

        public async Task<List<AlbumDto>> Handle(GetAlbumsByArtistQuery request, CancellationToken cancellationToken)
        {
            var albums = await _albumRepository.GetAlbumsByArtistAsync(request.ArtistId, cancellationToken);
            return _mapper.Map<List<AlbumDto>>(albums);
        }
    }
}