using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Tracks.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTracksByAlbumQueryHandler : IRequestHandler<GetTracksByAlbumQuery, List<TrackDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetTracksByAlbumQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<TrackDto>> Handle(GetTracksByAlbumQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Tracks
                .AsNoTracking()
                .Where(t => t.AlbumId == request.AlbumId)
                .Include(t => t.Album)
                .Include(t => t.Artist)
                .AsSplitQuery()
                .AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(t => t.CreatedById == request.UserId.Value);
            }
            
            if (request.SortByTrackNumber)
            {
                query = query.OrderBy(t => t.TrackNumber);
            }

            var tracks = await query.ToListAsync(cancellationToken);
            return _mapper.Map<List<TrackDto>>(tracks);
        }
    }
}
