using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Queries
{
    public class GetTrackByIdQueryHandler : IRequestHandler<GetTrackByIdQuery, TrackDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetTrackByIdQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<TrackDto?> Handle(GetTrackByIdQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Tracks
                .AsNoTracking()
                .Include(t => t.Album)
                .Include(t => t.Artist)
                .AsSplitQuery()
                .Where(t => t.Id == request.TrackId);

            if (request.UserId.HasValue)
            {
                query = query.Where(t => t.CreatedById == request.UserId.Value);
            }

            var track = await query.FirstOrDefaultAsync(cancellationToken);
            return track != null ? _mapper.Map<TrackDto>(track) : null;
        }
    }
}
