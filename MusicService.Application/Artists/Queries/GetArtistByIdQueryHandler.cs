using AutoMapper;
using MediatR;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Queries
{
    public class GetArtistByIdQueryHandler : IRequestHandler<GetArtistByIdQuery, ArtistDto?>
    {
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;

        public GetArtistByIdQueryHandler(
            IArtistRepository artistRepository,
            IMapper mapper)
        {
            _artistRepository = artistRepository;
            _mapper = mapper;
        }

        public async Task<ArtistDto?> Handle(GetArtistByIdQuery request, CancellationToken cancellationToken)
        {
            var artist = await _artistRepository.GetByIdAsync(request.ArtistId, cancellationToken);
            return artist != null ? _mapper.Map<ArtistDto>(artist) : null;
        }
    }
}