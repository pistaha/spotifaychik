using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Common.Interfaces;
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
            var track = await _dbContext.Tracks
                .AsNoTracking()
                .Include(t => t.Album)
                .Include(t => t.Artist)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == request.TrackId, cancellationToken);
            return track != null ? _mapper.Map<TrackDto>(track) : null;
        }
    }
}
