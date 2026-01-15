using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumByIdQueryHandler : IRequestHandler<GetAlbumByIdQuery, AlbumDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetAlbumByIdQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<AlbumDto?> Handle(GetAlbumByIdQuery request, CancellationToken cancellationToken)
        {
            var album = await _dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Artist)
                .Include(a => a.Tracks)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == request.AlbumId, cancellationToken);
            return album != null ? _mapper.Map<AlbumDto>(album) : null;
        }
    }
}
