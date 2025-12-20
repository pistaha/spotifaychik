using AutoMapper;
using MediatR;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumByIdQueryHandler : IRequestHandler<GetAlbumByIdQuery, AlbumDto?>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IMapper _mapper;

        public GetAlbumByIdQueryHandler(
            IAlbumRepository albumRepository,
            IMapper mapper)
        {
            _albumRepository = albumRepository;
            _mapper = mapper;
        }

        public async Task<AlbumDto?> Handle(GetAlbumByIdQuery request, CancellationToken cancellationToken)
        {
            var album = await _albumRepository.GetByIdAsync(request.AlbumId, cancellationToken);
            return album != null ? _mapper.Map<AlbumDto>(album) : null;
        }
    }
}