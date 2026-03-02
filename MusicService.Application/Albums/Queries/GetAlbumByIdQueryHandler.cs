using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Linq;
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
            var query = _dbContext.Albums
                .AsNoTracking()
                .Include(a => a.Artist)
                .Include(a => a.Tracks)
                .AsSplitQuery()
                .Where(a => a.Id == request.AlbumId);

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.CreatedById == request.UserId.Value);
            }

            var album = await query.FirstOrDefaultAsync(cancellationToken);
            return album != null ? _mapper.Map<AlbumDto>(album) : null;
        }
    }
}
