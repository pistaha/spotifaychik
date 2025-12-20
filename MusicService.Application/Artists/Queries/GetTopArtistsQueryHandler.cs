using AutoMapper;
using MediatR;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Queries
{
    public class GetTopArtistsQueryHandler : IRequestHandler<GetTopArtistsQuery, List<ArtistDto>>
    {
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;

        public GetTopArtistsQueryHandler(
            IArtistRepository artistRepository,
            IMapper mapper)
        {
            _artistRepository = artistRepository;
            _mapper = mapper;
        }

        public async Task<List<ArtistDto>> Handle(GetTopArtistsQuery request, CancellationToken cancellationToken)
        {
            var artists = await _artistRepository.GetTopArtistsAsync(request.Count, cancellationToken);
            return _mapper.Map<List<ArtistDto>>(artists);
        }
    }
}