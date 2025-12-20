using AutoMapper;
using MediatR;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Queries
{
    public class GetArtistsByGenreQueryHandler : IRequestHandler<GetArtistsByGenreQuery, List<ArtistDto>>
    {
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;

        public GetArtistsByGenreQueryHandler(
            IArtistRepository artistRepository,
            IMapper mapper)
        {
            _artistRepository = artistRepository;
            _mapper = mapper;
        }

        public async Task<List<ArtistDto>> Handle(GetArtistsByGenreQuery request, CancellationToken cancellationToken)
        {
            var artists = await _artistRepository.GetArtistsByGenreAsync(request.Genre, cancellationToken);
            return _mapper.Map<List<ArtistDto>>(artists);
        }
    }
}