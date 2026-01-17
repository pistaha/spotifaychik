using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumsByArtistQueryHandler : IRequestHandler<GetAlbumsByArtistQuery, List<AlbumDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetAlbumsByArtistQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<AlbumDto>> Handle(GetAlbumsByArtistQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Albums
                .AsNoTracking()
                .Where(a => a.ArtistId == request.ArtistId)
                .Include(a => a.Artist)
                .Include(a => a.Tracks)
                .AsSplitQuery()
                .AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.CreatedById == request.UserId.Value);
            }

            var albums = await query.ToListAsync(cancellationToken);
            return _mapper.Map<List<AlbumDto>>(albums);
        }
    }
}
